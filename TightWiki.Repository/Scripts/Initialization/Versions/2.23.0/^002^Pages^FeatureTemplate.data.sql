INSERT INTO FeatureTemplate
(
	Name,
	Type,
	PageId,
	Description,
	TemplateText
)
SELECT 'Alert','Scope Function','61','Alerts bring important information to the attention of the reader.','{{Alert(Primary, "Alert Title")

Alert style can be: default, primary, secondary, light, dark, success, info, warning, danger

This is the alert text.
}}
'
UNION ALL SELECT 'AppVersion','Standard Function','51','Displays the wiki engine version.','##AppVersion()
'
UNION ALL SELECT 'Attachments','Standard Function','44','Lists the attached files associated with a page.','##Attachments(Full, 10, true)
'
UNION ALL SELECT 'Background','Scope Function','21','Set the background color for a block of content.','{{Background(Primary)
Background style can be one of: default, primary, secondary, light, dark, success, info, warning, danger, muted.
	
This is the body content of the function scope.
}}
'
UNION ALL SELECT 'Blockquote','Scope Function','106','Blockquotes are good for quoting blocks of content from another source within your document.','{{Blockquote(Center, This is the caption)

The block style can be one of: default, start, center, end.

This is the content text.
}}
'
UNION ALL SELECT 'BR','Standard Function','41','Inserts one or more line-breaks into the wiki body.','##BR(10)
'
UNION ALL SELECT 'Bullets','Scope Function','18','Create hierarchical ordered or unordered lists.','{{Bullets
Bullet1
Bullet2
>SubBullet1
})
'
UNION ALL SELECT 'Callout','Scope Function','20','Prominently display useful information in a fairly understated panel.','{{Callout(Default, "Callout Title")
Note: Callout style can be: default, primary, secondary, success, info, warning, or danger.
Your callout text
}}
'
UNION ALL SELECT 'Card','Scope Function','23','Cards are good for displaying every-day content and can have a title.','{{Card(Default, "Card Title")
Note: Card style can be: default, primary, secondary, light, dark, success, info, warning, danger.
Your card text.
}}
'
UNION ALL SELECT 'Code','Scope Function','17','Display formatted, wiki-unprocessed and syntax-hilighted code.','{{Code()
SELECT
	*
FROM
    Employees as E
INNER JOIN Company as C
    ON C.Id = E.CompanyId
}}
'
UNION ALL SELECT 'Collapse','Scope Function','24','Dynamically show or hide a section of the wiki body.','{{Collapse(Show text example)
This is the content that was hidden.
This is the content that was hidden.
This is the content that was hidden.
}}
'
UNION ALL SELECT 'Color','Standard Function','27','Set the color of a portion of text.','##Color(#00AA00, This is green colored text)
'
UNION ALL SELECT 'Created','Standard Function','49','Displays the created date/time of the page.','##Created()
'
UNION ALL SELECT 'DefineSnippet','Scope Function','75','Sets a snippet variable to a block of wiki markup.','{{DefineSnippet(MyBlock)
This is the content of the snippet variable.
}}

##Snippet(MyBlock)
'
UNION ALL SELECT 'Deprecate Instruction','Instruction Function','52','Processing instruction indicating the page needs to be deleted.','@@Deprecate
'
UNION ALL SELECT 'Description (Standard Function)','Standard Function','105','Displays the description of the wiki page.','##Description
'
UNION ALL SELECT 'Draft Instruction','Instruction Function','57','Processing instruction indicating the page is in draft - is incomplete.','@@Draft

'
UNION ALL SELECT 'EditLink','Standard Function','39','Insert a link to quickly edit the page.','##EditLink(Edit this page)
'
UNION ALL SELECT 'Figure','Scope Function','104','Displays a piece of content along with an optional caption, especially useful for images.','{{Figure(Center, This is the caption)
##Image(Builtin :: Media/TightWiki Logo.png, :scale=100, :class=img-fluid)
}}
'
UNION ALL SELECT 'File','Standard Function','37','Insert file download links into the wiki body.','##File(Example Attachment 1.txt, Download now!, true)
'
UNION ALL SELECT 'Foreground','Scope Function','22','Set the foreground color for a block of content.','{{Foreground(Primary)
Foreground style can be one of: default, primary, secondary, light, dark, success, info, warning, danger, muted.

This is the body content of the function scope.
}}
'
UNION ALL SELECT 'Get','Standard Function','26','Get wiki page variable value.','${variableName = This is the variable value}
${variableName}
'
UNION ALL SELECT 'HideFooterComments Instruction','Instruction Function','85','Indicates that comments should not display comments at the bottom regardless of the system settings.','@@HideFooterComments
'
UNION ALL SELECT 'HR','Standard Function','42','Inserts a horizontal rule (divider) into the wiki bosy.','##HR(1)
'
UNION ALL SELECT 'Image','Standard Function','36','Display images in the wiki body.','##Image(Builtin :: Media/TightWiki Logo.png, 100)
'
UNION ALL SELECT 'Include','Standard Function','58','Inserts the processed wiki body of one page into the calling page.','##Include(pageName)
'
UNION ALL SELECT 'Include Instruction','Instruction Function','56','Processing instruction indicating the page is an include and not a while page.','@@Include
'
UNION ALL SELECT 'Inject','Standard Function','40','Inserts the un-processed wiki body of one page into the calling page.','##Inject(pageName)
'
UNION ALL SELECT 'Jumbotron','Scope Function','19','Large grey box good for calling attention in a non-critical way.','{{Jumbotron
This is the body content of the function scope.
}}
'
UNION ALL SELECT 'LastModified','Standard Function','50','Displays the modified date/time of the page.','##LastModified
'
UNION ALL SELECT 'Literals','Block','3','Literal blocks for displaying verbatim content.','#{
your text here and code here
}#
'
UNION ALL SELECT 'Name','Standard Function','48','Displays the pages name.','##Name
'
UNION ALL SELECT 'Namespace','Standard Function','65','Displays the namespace of the wiki page.','##Namespace
'
UNION ALL SELECT 'NamespaceGlossary','Standard Function','64','Create a glossary from of pages in specified namespaces.','##NamespaceGlossary("Wiki Help, Other Help", 10)
'
UNION ALL SELECT 'NamespaceList','Standard Function','63','Create a list of pages from specified namespaces.','##NamespaceList("Wiki Help, Other Help", 20)
'
UNION ALL SELECT 'Navigation','Standard Function','47','Diaplys the URL friendly navigation name of a page.','##Navigation
'
UNION ALL SELECT 'NoCache Instruction','Instruction Function','72','Processing instruction indicating that the page should never be cached.','@@NoCache
'
UNION ALL SELECT 'Order','Scope Function','73','Orders the resulting lines of a block of wiki markup.','{{Order
This
Is
A
List
Of
Lines
To
Sort
}}
'
UNION ALL SELECT 'Protect Instruction','Instruction Function','53','Processing instruction indicating the page is protected from unauthorized modification.','@@Protect(false)
'
UNION ALL SELECT 'RecentlyModified','Standard Function','33','Display a list of recently modified wiki pages.','##RecentlyModified(10, Full, True)
'
UNION ALL SELECT 'Related','Standard Function','59','Create a list of related pages based on incoming links.','##Related(List, 20, False)
'
UNION ALL SELECT 'Review Instruction','Instruction Function','55','Processing instruction indicating the page needs to be reviewed.','@@Review
'
UNION ALL SELECT 'Revisions','Standard Function','43','Displays the revision history of a page.','##Revisions(Full, 10, true)
'
UNION ALL SELECT 'SearchCloud','Standard Function','31','Create a cloud of pages from a search of wiki body contents.','##SearchCloud("Wiki help color", 10)
'
UNION ALL SELECT 'SearchList','Standard Function','29','Create  a list from a search of wiki body contents.','##SearchList("Wiki help color")
'
UNION ALL SELECT 'Seq','Standard Function','62','Automatically create a numbered sequence on a page.','**##Seq** - This is my first point.
You can have content between the sequences.
**##Seq** - This is my second point.
You can have additional content between the sequences.
**##Seq** - This is my third point.
'
UNION ALL SELECT 'Set','Standard Function','25','Set wiki page variable value.','${variableName = value}

${variableName}
'
UNION ALL SELECT 'Similar','Standard Function','60','Create a list of similar pages based on tag similarities.','##Similar(20)
'
UNION ALL SELECT 'SiteName','Standard Function','103','Displays the name of the configured site.','##SiteName
'
UNION ALL SELECT 'Snippet','Standard Function','76','Injects the value of a previously defined snippet variable.','{{DefineSnippet(MyBlock)
This is the content of the snippet variable.
}}

##Snippet(MyBlock)
##Snippet(MyBlock)
##Snippet(MyBlock)
'
UNION ALL SELECT 'StripedTable','Scope Function','74','The StripedTable scope function allows you to create tables with columns and alternating colored rows.','{{StripedTable
Column #1 || Column #2 || Column #3 || Column #4
Row #1 || Cell #1 || Cell #2 || Cell #3
Row #2 || Cell #1 || Cell #2 || Cell #3
Row #3 || Cell #1 || Cell #2 || Cell #3
Row #4 || Cell #1 || Cell #2 || Cell #3
}}
'
UNION ALL SELECT 'Table','Scope Function','66','The Table scope function allows you to create tables with rows and columns.','{{Table
Column #1 || Column #2 || Column #3 || Column #4
Row #1 || Cell #1 || Cell #2 || Cell #3
Row #2 || Cell #1 || Cell #2 || Cell #3
Row #3 || Cell #1 || Cell #2 || Cell #3
Row #4 || Cell #1 || Cell #2 || Cell #3
}}
'
UNION ALL SELECT 'TagCloud','Standard Function','35','Create a tag cloud from a seed tag.','##TagCloud("Help", 100)
'
UNION ALL SELECT 'TagGlossary','Standard Function','32','Create a glossary from a search of specified tags.','##TagGlossary("Official-Help, help", 5)
'
UNION ALL SELECT 'TagList','Standard Function','30','Create a list of pages from a search of page tags.','##TagList("Official-Help, help")
'
UNION ALL SELECT 'Tags (Instruction Function)','Instruction Function','28','Associate tags with a page.','##Tags(These, are, the, tags, we, want, to, add)
'
UNION ALL SELECT 'Tags (Standard Function)','Instruction Function','38','Display the tags associated with the page.','##Tags
'
UNION ALL SELECT 'Template Instruction','Instruction Function','54','Processing instruction indicating the page is a template.','@@Template
'
UNION ALL SELECT 'TextGlossary','Standard Function','34','Create a glossary from a search of wiki body contens.','##TextGlossary("Wiki help", 10, Full)
'
UNION ALL SELECT 'Title (Instruction Function)','Instruction Function','102','Sets the title of the page for the browser (HTML head title).','##Title("My Wiki Site")
'
UNION ALL SELECT 'Title (Standard Function)','Standard Function','46','Displays the title of the wiki page in title format.','##Title
'
UNION ALL SELECT 'TOC','Standard Function','45','Generate a table-of-contents for a page from the headings.','##TOC
';
