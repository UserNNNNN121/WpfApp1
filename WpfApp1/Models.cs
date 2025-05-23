using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace WpfApp1
{
    /// <summary>
    /// Класс Module представляет модуль курса с идентификатором, названием, описанием,
    /// порядковым индексом и связью с курсом через CourseId
    /// </summary>
    public class Module
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int OrderIndex { get; set; }
        public int CourseId { get; set; } 
    }
    /// <summary>
    /// Класс ModuleItem представляет элемент внутри модуля курса:
    /// содержит информацию о типе, названии, контенте, длительности и статусе прохождения
    /// </summary>
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
        public bool IsCompleted { get; set; } 
    }
    /// <summary>
    /// Частичный класс CourseModel содержит основные свойства курса:
    /// идентификаторы, название, описание, срок доступности и связь с другими сущностями
    /// </summary>
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
    /// <summary>
    /// Класс TestData представляет структуру теста:
    /// содержит общее количество баллов и список вопросов
    /// </summary>
    public class TestData
    {
        public int TotalPoints { get; set; } = 100;
        public List<TestQuestion> Questions { get; set; }
    }
    /// <summary>
    /// Класс TestQuestion описывает отдельный вопрос в тесте:
    /// включает текст вопроса, тип, баллы, порядковый номер и список ответов
    /// </summary>
    public class TestQuestion
    {
        public string Text { get; set; }
        public string Type { get; set; }
        public int Points { get; set; }
        public int OrderIndex { get; set; }
        public List<TestAnswer> Answers { get; set; }
    }
    /// <summary>
    /// Класс TestAnswer представляет ответ на тестовый вопрос:
    /// содержит текст ответа, правильность и порядковый номер
    /// </summary>
    public class TestAnswer
    {
        public string Text { get; set; }
        public bool IsCorrect { get; set; }
        public int OrderIndex { get; set; }
    }
}
