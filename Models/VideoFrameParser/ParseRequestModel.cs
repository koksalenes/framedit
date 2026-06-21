using System.ComponentModel.DataAnnotations;
using Mediaration.Constants;

namespace Mediaration.Models.VideoFrameParser;

public class ParseRequestModel
{
    [Required]
    public IFormFile? VideoFile { get; set; }

    public double Fps { get; set; } = 0;

    [Required]
    [MaxLength(120)]
    [RegularExpression(AppConstants.VideoFrameParser.BaseNamePattern,
        ErrorMessage = "Base name may only contain letters, numbers, hyphens (-) and underscores (_), and must start with a letter or number.")]
    public string BaseName { get; set; } = "frame";

    public string Format { get; set; } = AppConstants.Ext.Jpg.Name;

    public bool StripMetadata { get; set; } = false;
}
