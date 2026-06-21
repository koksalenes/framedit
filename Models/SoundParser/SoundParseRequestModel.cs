using System.ComponentModel.DataAnnotations;
using Framedit.Constants;

namespace Framedit.Models.SoundParser;

public class SoundParseRequestModel
{
    [Required]
    public IFormFile? VideoFile { get; set; }

    public string Format { get; set; } = AppConstants.Ext.Mp3.Name;
}
