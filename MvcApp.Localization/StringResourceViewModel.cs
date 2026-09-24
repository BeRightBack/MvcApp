using MvcApp.Core;

namespace MvcApp.Localization
{
    public class StringResourceViewModel
    {
        public int Id { get; set; }
        public int LanguageId { get; set; }
        public string? Name { get; set; }
        public string? Value { get; set; }

        public StringResource ToEntity()
        {
            return new StringResource
            {
                Id = Id,
                LanguageId = LanguageId,
                Name = Name,
                Value = Value
            };
        }
    }
}