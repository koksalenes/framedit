using System.ComponentModel.DataAnnotations;
using Mediaration.Constants;

namespace Mediaration.Models.SoundParser;

public class SoundParseRequestModel
{
    [Required]
    public IFormFile? VideoFile { get; set; }

    public string Format { get; set; } = AppConstants.Ext.Mp3.Name;
}
