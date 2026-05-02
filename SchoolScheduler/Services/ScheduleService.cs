using Microsoft.EntityFrameworkCore;
using SchoolScheduler.Data;
using SchoolScheduler.Models;

namespace SchoolScheduler.Services
{
    public class ScheduleService
    {
        private readonly SchoolDbContext _context;

        // In-memory tracking
        private Dictionary<int, int> teacherHours = new();
        private Dictionary<(int teacherId, string day), int> teacherDailyHours = new();
        private Dictionary<int, int> classHours = new();

        public ScheduleService(SchoolDbContext context)
        {
            _context = context;
        }

        public async Task<string> GenerateWeeklyScheduleAsync()
        {
            // Clear old data
            _context.Schedules.RemoveRange(_context.Schedules);
            await _context.SaveChangesAsync();

            // Load data
            var teachers = await _context.Teachers.ToListAsync();
            var classSubjects = await _context.ClassSubjects.ToListAsync();
            var timeSlots = await _context.TimeSlots.ToListAsync();

            // Build requirements (REAL FIX: use RequiredHours)
            var requirements = new List<LessonRequirement>();

            foreach (var cs in classSubjects)
            {
                for (int i = 0; i < cs.RequiredHours; i++)
                {
                    requirements.Add(new LessonRequirement
                    {
                        ClassID = cs.ClassID,
                        SubjectID = cs.SubjectID
                    });
                }
            }

            // 🔥 MRV: sort by difficulty (least available teachers first)
            var teacherSubjects = await _context.TeacherSubjects.ToListAsync();

            requirements = requirements
                .OrderBy(r => teacherSubjects.Count(ts => ts.SubjectID == r.SubjectID))
                .ToList();

            // Start solving
            bool success = await SolveAsync(requirements, 0, timeSlots, teachers);

            if (success)
            {
                await _context.SaveChangesAsync();
                return "SUCCESS";
            }

            return "FAILED";
        }

        // ================================
        // BACKTRACKING
        // ================================
        private async Task<bool> SolveAsync(
            List<LessonRequirement> req,
            int index,
            List<TimeSlot> slots,
            List<Teacher> teachers)
        {
            // DONE
            if (index == req.Count)
                return true;

            var current = req[index];

            // 🔁 Try all teachers
            foreach (var teacher in teachers)
            {
                // Can teach?
                bool canTeach = await _context.TeacherSubjects
                    .AnyAsync(t => t.TeacherID == teacher.TeacherID &&
                                   t.SubjectID == current.SubjectID);

                if (!canTeach)
                    continue;

                // 🔁 Try all slots
                foreach (var slot in slots)
                {
                    // 🔥 Forward Checking (basic)
                    if (!await HasFuturePossibility(current, teachers, slots))
                        return false;

                    if (!await IsValidAsync(current.ClassID, teacher, slot))
                        continue;

                    // Assign
                    var schedule = new Schedule
                    {
                        ClassID = current.ClassID,
                        SubjectID = current.SubjectID,
                        TeacherID = teacher.TeacherID,
                        TimeSlotID = slot.TimeSlotID
                    };

                    _context.Schedules.Add(schedule);
                    UpdateTracking(teacher.TeacherID, slot, current.ClassID, +1);

                    // Recurse
                    if (await SolveAsync(req, index + 1, slots, teachers))
                        return true;

                    // BACKTRACK
                    _context.Schedules.Remove(schedule);
                    UpdateTracking(teacher.TeacherID, slot, current.ClassID, -1);
                }
            }

            return false;
        }

        // ================================
        // CONSTRAINTS
        // ================================
        private async Task<bool> IsValidAsync(int classId, Teacher teacher, TimeSlot slot)
        {
            // Class conflict
            if (await _context.Schedules
                .AnyAsync(s => s.ClassID == classId && s.TimeSlotID == slot.TimeSlotID))
                return false;

            // Teacher conflict
            if (await _context.Schedules
                .AnyAsync(s => s.TeacherID == teacher.TeacherID && s.TimeSlotID == slot.TimeSlotID))
                return false;

            // Availability
            bool available = await _context.TeacherAvailabilities
                .AnyAsync(a => a.TeacherID == teacher.TeacherID &&
                               a.TimeSlotID == slot.TimeSlotID &&
                               a.IsAvailable);

            if (!available)
                return false;

            // Weekly limit
            teacherHours.TryGetValue(teacher.TeacherID, out int hours);
            if (hours >= teacher.MaxWeeklyHours)
                return false;

            // Daily break (max 5)
            var key = (teacher.TeacherID, slot.Day);
            teacherDailyHours.TryGetValue(key, out int daily);
            if (daily >= 5)
                return false;

            // Class must not exceed 30
            classHours.TryGetValue(classId, out int ch);
            if (ch >= 30)
                return false;

            return true;
        }

        // ================================
        // FORWARD CHECKING
        // ================================
        private async Task<bool> HasFuturePossibility(
            LessonRequirement current,
            List<Teacher> teachers,
            List<TimeSlot> slots)
        {
            // Check if at least ONE valid assignment exists
            foreach (var teacher in teachers)
            {
                bool canTeach = await _context.TeacherSubjects
                    .AnyAsync(t => t.TeacherID == teacher.TeacherID &&
                                   t.SubjectID == current.SubjectID);

                if (!canTeach)
                    continue;

                foreach (var slot in slots)
                {
                    if (await IsValidAsync(current.ClassID, teacher, slot))
                        return true;
                }
            }

            return false;
        }

        // ================================
        // TRACKING
        // ================================
        private void UpdateTracking(int teacherId, TimeSlot slot, int classId, int delta)
        {
            if (!teacherHours.ContainsKey(teacherId))
                teacherHours[teacherId] = 0;

            teacherHours[teacherId] += delta;

            var key = (teacherId, slot.Day);

            if (!teacherDailyHours.ContainsKey(key))
                teacherDailyHours[key] = 0;

            teacherDailyHours[key] += delta;

            if (!classHours.ContainsKey(classId))
                classHours[classId] = 0;

            classHours[classId] += delta;
        }
    }
}
public class LessonRequirement
{
    public int ClassID { get; set; }
    public int SubjectID { get; set; }
}