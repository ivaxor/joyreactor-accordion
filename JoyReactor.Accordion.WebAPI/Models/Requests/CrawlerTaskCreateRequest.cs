using JoyReactor.Accordion.Logic.ApiClient.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace JoyReactor.Accordion.WebAPI.Models.Requests;

public record CrawlerTaskCreateRequest : IModelValidator
{
    [Required]
    public bool IsIndefinite { get; set; }

    [Required]
    [MaxLength(100)]
    public string TagName { get; set; }

    [Required]
    [DefaultValue(PostLineType.ALL)]
    public PostLineType PostLineType { get; set; }

    public int? PageFrom { get; set; }
    public int? PageTo { get; set; }

    public IEnumerable<ModelValidationResult> Validate(ModelValidationContext context)
    {
        if (PageFrom != null && PageTo != null && PageFrom > PageTo)
            yield return new ModelValidationResult(nameof(PageFrom), "PageFrom should be always less then PageTo");
    }
}