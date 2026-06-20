using System.ComponentModel.DataAnnotations;

namespace Framedit.Models.SoundParser;

public class SoundParseRequestModel
{
    [Required]
    public IFormFile? VideoFile { get; set; }

    public string Format { get; set; } = "mp3";
}
