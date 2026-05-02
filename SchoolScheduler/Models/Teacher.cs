namespace SchoolScheduler.Models
{
    public class Teacher
    {
        public int TeacherID { get; set; }
        public string? TeacherName { get; set; }
        public int MaxWeeklyHours { get; set; } = 25;
    }
}