using System;
using System.Collections.Generic;

namespace TightWiki.Data.EfCore._Scaffold.Pages;

public partial class Page
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Namespace { get; set; } = null!;

    public string Navigation { get; set; } = null!;

    public string Description { get; set; } = null!;

    public int Revision { get; set; }

    public Guid CreatedByUserId { get; set; }

    public DateTime CreatedDate { get; set; }

    public Guid ModifiedByUserId { get; set; }

    public DateTime ModifiedDate { get; set; }

    public virtual ICollection<FeatureTemplate> FeatureTemplates { get; set; } = new List<FeatureTemplate>();

    public virtual ICollection<PageComment> PageComments { get; set; } = new List<PageComment>();

    public virtual ICollection<PageFile> PageFiles { get; set; } = new List<PageFile>();

    public virtual ICollection<PageProcessingInstruction> PageProcessingInstructions { get; set; } = new List<PageProcessingInstruction>();

    public virtual ICollection<PageReference> PageReferencePages { get; set; } = new List<PageReference>();

    public virtual ICollection<PageReference> PageReferenceReferencesPages { get; set; } = new List<PageReference>();

    public virtual ICollection<PageRevisionAttachment> PageRevisionAttachments { get; set; } = new List<PageRevisionAttachment>();

    public virtual ICollection<PageTag> PageTags { get; set; } = new List<PageTag>();

    public virtual ICollection<PageToken> PageTokens { get; set; } = new List<PageToken>();
}
