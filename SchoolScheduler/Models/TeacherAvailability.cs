using System.ComponentModel.DataAnnotations;    
namespace SchoolScheduler.Models 
{
    public class TeacherAvailability
    {
        [Key] 
        public int AvailabilityID { get; set; }
        public int TeacherID { get; set; }
        public int TimeSlotID { get; set; }
        public bool IsAvailable { get; set; }
    }
}