using System.ComponentModel.DataAnnotations;
using Framedit.Constants;

namespace Framedit.Models.ImageOptimizer;

public class ImageOptimizeRequestModel
{
    public IFormFileCollection? Files { get; set; }

    public string OutputFormat { get; set; } = AppConstants.ImageOptimizer.KeepFormat;

    [Range(1, 100)]
    public int Quality { get; set; } = 85;

    public bool StripMetadata { get; set; } = false;
}
