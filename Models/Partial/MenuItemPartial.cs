using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace AiBotOrderingSystem.Models.DbFirst;

public partial class MenuItem
{
    // Add this back (not part of DB)
    [NotMapped]
    public IFormFile? ImageFile { get; set; }

    //public string? Addon { get; set; }

}
