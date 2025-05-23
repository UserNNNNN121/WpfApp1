using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace WpfApp1
{
    public class Module
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int OrderIndex { get; set; }
        public int CourseId { get; set; } // Add this line
    }

    public class ModuleItem
    {
        public int Id { get; set; }
        public int ModuleId { get; set; }
        public string ItemType { get; set; }
        public string Title { get; set; }
        public string ContentPath { get; set; }
        public string ExternalUrl { get; set; }
        public int? DurationMinutes { get; set; }
        public int OrderIndex { get; set; }
        public bool IsCompleted { get; set; } // Added this property
    }
    public partial class CourseModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int SpecialityId { get; set; }
        public int AvailabilityId { get; set; }
        public int PartnerId { get; set; }
        public DateTime? AvailableUntil { get; set; }
    }
    public class TestData
    {
        public int TotalPoints { get; set; } = 100;
        public List<TestQuestion> Questions { get; set; }
    }

    public class TestQuestion
    {
        public string Text { get; set; }
        public string Type { get; set; }
        public int Points { get; set; }
        public int OrderIndex { get; set; }
        public List<TestAnswer> Answers { get; set; }
    }

    public class TestAnswer
    {
        public string Text { get; set; }
        public bool IsCorrect { get; set; }
        public int OrderIndex { get; set; }
    }
}
