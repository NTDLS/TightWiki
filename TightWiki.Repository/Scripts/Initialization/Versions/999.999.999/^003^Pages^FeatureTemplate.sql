INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'Alert', 'Scope Function', (SELECT Id FROM Page WHERE Name = '61' LIMIT 1), 'Alerts bring important information to the attention of the reader.', '{{Alert(Primary, "Alert Title")

Alert style can be: default, primary, secondary, light, dark, success, info, warning, danger

This is the alert text.
}}
'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Scope Function', Description = 'Alerts bring important information to the attention of the reader.', TemplateText = '{{Alert(Primary, "Alert Title")

Alert style can be: default, primary, secondary, light, dark, success, info, warning, danger

This is the alert text.
}}
', PageId = (SELECT Id FROM Page WHERE Name = '61' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'AppVersion', 'Standard Function', (SELECT Id FROM Page WHERE Name = '51' LIMIT 1), 'Displays the wiki engine version.', '##AppVersion()
'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Standard Function', Description = 'Displays the wiki engine version.', TemplateText = '##AppVersion()
', PageId = (SELECT Id FROM Page WHERE Name = '51' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'Attachments', 'Standard Function', (SELECT Id FROM Page WHERE Name = '44' LIMIT 1), 'Lists the attached files associated with a page.', '##Attachments(Full, 10, true)
'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Standard Function', Description = 'Lists the attached files associated with a page.', TemplateText = '##Attachments(Full, 10, true)
', PageId = (SELECT Id FROM Page WHERE Name = '44' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'Background', 'Scope Function', (SELECT Id FROM Page WHERE Name = '21' LIMIT 1), 'Set the background color for a block of content.', '{{Background(Primary)
Background style can be one of: default, primary, secondary, light, dark, success, info, warning, danger, muted.
	
This is the body content of the function scope.
}}
'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Scope Function', Description = 'Set the background color for a block of content.', TemplateText = '{{Background(Primary)
Background style can be one of: default, primary, secondary, light, dark, success, info, warning, danger, muted.
	
This is the body content of the function scope.
}}
', PageId = (SELECT Id FROM Page WHERE Name = '21' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'Blockquote', 'Scope Function', (SELECT Id FROM Page WHERE Name = '106' LIMIT 1), 'Blockquotes are good for quoting blocks of content from another source within your document.', '{{Blockquote(Center, This is the caption)

The block style can be one of: default, start, center, end.

This is the content text.
}}
'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Scope Function', Description = 'Blockquotes are good for quoting blocks of content from another source within your document.', TemplateText = '{{Blockquote(Center, This is the caption)

The block style can be one of: default, start, center, end.

This is the content text.
}}
', PageId = (SELECT Id FROM Page WHERE Name = '106' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'BR', 'Standard Function', (SELECT Id FROM Page WHERE Name = '41' LIMIT 1), 'Inserts one or more line-breaks into the wiki body.', '##BR(10)
'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Standard Function', Description = 'Inserts one or more line-breaks into the wiki body.', TemplateText = '##BR(10)
', PageId = (SELECT Id FROM Page WHERE Name = '41' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'Bullets', 'Scope Function', (SELECT Id FROM Page WHERE Name = '18' LIMIT 1), 'Create hierarchical ordered or unordered lists.', '{{Bullets
Bullet1
Bullet2
>SubBullet1
})
'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Scope Function', Description = 'Create hierarchical ordered or unordered lists.', TemplateText = '{{Bullets
Bullet1
Bullet2
>SubBullet1
})
', PageId = (SELECT Id FROM Page WHERE Name = '18' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'Callout', 'Scope Function', (SELECT Id FROM Page WHERE Name = '20' LIMIT 1), 'Prominently display useful information in a fairly understated panel.', '{{Callout(Default, "Callout Title")
Note: Callout style can be: default, primary, secondary, success, info, warning, or danger.
Your callout text
}}
'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Scope Function', Description = 'Prominently display useful information in a fairly understated panel.', TemplateText = '{{Callout(Default, "Callout Title")
Note: Callout style can be: default, primary, secondary, success, info, warning, or danger.
Your callout text
}}
', PageId = (SELECT Id FROM Page WHERE Name = '20' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'Card', 'Scope Function', (SELECT Id FROM Page WHERE Name = '23' LIMIT 1), 'Cards are good for displaying every-day content and can have a title.', '{{Card(Default, "Card Title")
Note: Card style can be: default, primary, secondary, light, dark, success, info, warning, danger.
Your card text.
}}
'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Scope Function', Description = 'Cards are good for displaying every-day content and can have a title.', TemplateText = '{{Card(Default, "Card Title")
Note: Card style can be: default, primary, secondary, light, dark, success, info, warning, danger.
Your card text.
}}
', PageId = (SELECT Id FROM Page WHERE Name = '23' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'Code', 'Scope Function', (SELECT Id FROM Page WHERE Name = '17' LIMIT 1), 'Display formatted, wiki-unprocessed and syntax-hilighted code.', '{{Code()
SELECT
	*
FROM
    Employees as E
INNER JOIN Company as C
    ON C.Id = E.CompanyId
}}
'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Scope Function', Description = 'Display formatted, wiki-unprocessed and syntax-hilighted code.', TemplateText = '{{Code()
SELECT
	*
FROM
    Employees as E
INNER JOIN Company as C
    ON C.Id = E.CompanyId
}}
', PageId = (SELECT Id FROM Page WHERE Name = '17' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'Collapse', 'Scope Function', (SELECT Id FROM Page WHERE Name = '24' LIMIT 1), 'Dynamically show or hide a section of the wiki body.', '{{Collapse(Show text example)
This is the content that was hidden.
This is the content that was hidden.
This is the content that was hidden.
}}
'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Scope Function', Description = 'Dynamically show or hide a section of the wiki body.', TemplateText = '{{Collapse(Show text example)
This is the content that was hidden.
This is the content that was hidden.
This is the content that was hidden.
}}
', PageId = (SELECT Id FROM Page WHERE Name = '24' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'Color', 'Standard Function', (SELECT Id FROM Page WHERE Name = '27' LIMIT 1), 'Set the color of a portion of text.', '##Color(#00AA00, This is green colored text)
'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Standard Function', Description = 'Set the color of a portion of text.', TemplateText = '##Color(#00AA00, This is green colored text)
', PageId = (SELECT Id FROM Page WHERE Name = '27' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'Created', 'Standard Function', (SELECT Id FROM Page WHERE Name = '49' LIMIT 1), 'Displays the created date/time of the page.', '##Created()
'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Standard Function', Description = 'Displays the created date/time of the page.', TemplateText = '##Created()
', PageId = (SELECT Id FROM Page WHERE Name = '49' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'DefineSnippet', 'Scope Function', (SELECT Id FROM Page WHERE Name = '75' LIMIT 1), 'Sets a snippet variable to a block of wiki markup.', '{{DefineSnippet(MyBlock)
This is the content of the snippet variable.
}}

##Snippet(MyBlock)
'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Scope Function', Description = 'Sets a snippet variable to a block of wiki markup.', TemplateText = '{{DefineSnippet(MyBlock)
This is the content of the snippet variable.
}}

##Snippet(MyBlock)
', PageId = (SELECT Id FROM Page WHERE Name = '75' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'Deprecate', 'Instruction Function', (SELECT Id FROM Page WHERE Name = '52' LIMIT 1), 'Processing instruction indicating the page needs to be deleted.', '@@Deprecate
'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Instruction Function', Description = 'Processing instruction indicating the page needs to be deleted.', TemplateText = '@@Deprecate
', PageId = (SELECT Id FROM Page WHERE Name = '52' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'Description', 'Standard Function', (SELECT Id FROM Page WHERE Name = '105' LIMIT 1), 'Displays the description of the wiki page.', '##Description
'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Standard Function', Description = 'Displays the description of the wiki page.', TemplateText = '##Description
', PageId = (SELECT Id FROM Page WHERE Name = '105' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'Draft', 'Instruction Function', (SELECT Id FROM Page WHERE Name = '57' LIMIT 1), 'Processing instruction indicating the page is in draft - is incomplete.', '@@Draft

'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Instruction Function', Description = 'Processing instruction indicating the page is in draft - is incomplete.', TemplateText = '@@Draft

', PageId = (SELECT Id FROM Page WHERE Name = '57' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'EditLink', 'Standard Function', (SELECT Id FROM Page WHERE Name = '39' LIMIT 1), 'Insert a link to quickly edit the page.', '##EditLink(Edit this page)
'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Standard Function', Description = 'Insert a link to quickly edit the page.', TemplateText = '##EditLink(Edit this page)
', PageId = (SELECT Id FROM Page WHERE Name = '39' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'Figure', 'Scope Function', (SELECT Id FROM Page WHERE Name = '104' LIMIT 1), 'Displays a piece of content along with an optional caption, especially useful for images.', '{{Figure(Center, This is the caption)
##Image(Builtin :: Media/TightWiki Logo.png, :scale=100, :class=img-fluid)
}}
'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Scope Function', Description = 'Displays a piece of content along with an optional caption, especially useful for images.', TemplateText = '{{Figure(Center, This is the caption)
##Image(Builtin :: Media/TightWiki Logo.png, :scale=100, :class=img-fluid)
}}
', PageId = (SELECT Id FROM Page WHERE Name = '104' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'File', 'Standard Function', (SELECT Id FROM Page WHERE Name = '37' LIMIT 1), 'Insert file download links into the wiki body.', '##File(Example Attachment 1.txt, Download now!, true)
'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Standard Function', Description = 'Insert file download links into the wiki body.', TemplateText = '##File(Example Attachment 1.txt, Download now!, true)
', PageId = (SELECT Id FROM Page WHERE Name = '37' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'Foreground', 'Scope Function', (SELECT Id FROM Page WHERE Name = '22' LIMIT 1), 'Set the foreground color for a block of content.', '{{Foreground(Primary)
Foreground style can be one of: default, primary, secondary, light, dark, success, info, warning, danger, muted.

This is the body content of the function scope.
}}
'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Scope Function', Description = 'Set the foreground color for a block of content.', TemplateText = '{{Foreground(Primary)
Foreground style can be one of: default, primary, secondary, light, dark, success, info, warning, danger, muted.

This is the body content of the function scope.
}}
', PageId = (SELECT Id FROM Page WHERE Name = '22' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'Get', 'Standard Function', (SELECT Id FROM Page WHERE Name = '26' LIMIT 1), 'Get wiki page variable value.', '${variableName = This is the variable value}
${variableName}
'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Standard Function', Description = 'Get wiki page variable value.', TemplateText = '${variableName = This is the variable value}
${variableName}
', PageId = (SELECT Id FROM Page WHERE Name = '26' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'HideFooterComments', 'Instruction Function', (SELECT Id FROM Page WHERE Name = '85' LIMIT 1), 'Indicates that comments should not display comments at the bottom regardless of the system settings.', '@@HideFooterComments
'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Instruction Function', Description = 'Indicates that comments should not display comments at the bottom regardless of the system settings.', TemplateText = '@@HideFooterComments
', PageId = (SELECT Id FROM Page WHERE Name = '85' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'HR', 'Standard Function', (SELECT Id FROM Page WHERE Name = '42' LIMIT 1), 'Inserts a horizontal rule (divider) into the wiki bosy.', '##HR(1)
'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Standard Function', Description = 'Inserts a horizontal rule (divider) into the wiki bosy.', TemplateText = '##HR(1)
', PageId = (SELECT Id FROM Page WHERE Name = '42' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'Image', 'Standard Function', (SELECT Id FROM Page WHERE Name = '36' LIMIT 1), 'Display images in the wiki body.', '##Image(Builtin :: Media/TightWiki Logo.png, 100)
'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Standard Function', Description = 'Display images in the wiki body.', TemplateText = '##Image(Builtin :: Media/TightWiki Logo.png, 100)
', PageId = (SELECT Id FROM Page WHERE Name = '36' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'Include', 'Standard Function', (SELECT Id FROM Page WHERE Name = '58' LIMIT 1), 'Inserts the processed wiki body of one page into the calling page.', '##Include(pageName)
'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Standard Function', Description = 'Inserts the processed wiki body of one page into the calling page.', TemplateText = '##Include(pageName)
', PageId = (SELECT Id FROM Page WHERE Name = '58' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'Include', 'Instruction Function', (SELECT Id FROM Page WHERE Name = '56' LIMIT 1), 'Processing instruction indicating the page is an include and not a while page.', '@@Include
'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Instruction Function', Description = 'Processing instruction indicating the page is an include and not a while page.', TemplateText = '@@Include
', PageId = (SELECT Id FROM Page WHERE Name = '56' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'Inject', 'Standard Function', (SELECT Id FROM Page WHERE Name = '40' LIMIT 1), 'Inserts the un-processed wiki body of one page into the calling page.', '##Inject(pageName)
'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Standard Function', Description = 'Inserts the un-processed wiki body of one page into the calling page.', TemplateText = '##Inject(pageName)
', PageId = (SELECT Id FROM Page WHERE Name = '40' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'Jumbotron', 'Scope Function', (SELECT Id FROM Page WHERE Name = '19' LIMIT 1), 'Large grey box good for calling attention in a non-critical way.', '{{Jumbotron
This is the body content of the function scope.
}}
'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Scope Function', Description = 'Large grey box good for calling attention in a non-critical way.', TemplateText = '{{Jumbotron
This is the body content of the function scope.
}}
', PageId = (SELECT Id FROM Page WHERE Name = '19' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'LastModified', 'Standard Function', (SELECT Id FROM Page WHERE Name = '50' LIMIT 1), 'Displays the modified date/time of the page.', '##LastModified
'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Standard Function', Description = 'Displays the modified date/time of the page.', TemplateText = '##LastModified
', PageId = (SELECT Id FROM Page WHERE Name = '50' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'Literals', 'Block', (SELECT Id FROM Page WHERE Name = '3' LIMIT 1), 'Literal blocks for displaying verbatim content.', '#{
your text here and code here
}#
'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Block', Description = 'Literal blocks for displaying verbatim content.', TemplateText = '#{
your text here and code here
}#
', PageId = (SELECT Id FROM Page WHERE Name = '3' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'Name', 'Standard Function', (SELECT Id FROM Page WHERE Name = '48' LIMIT 1), 'Displays the pages name.', '##Name
'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Standard Function', Description = 'Displays the pages name.', TemplateText = '##Name
', PageId = (SELECT Id FROM Page WHERE Name = '48' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'Namespace', 'Standard Function', (SELECT Id FROM Page WHERE Name = '65' LIMIT 1), 'Displays the namespace of the wiki page.', '##Namespace
'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Standard Function', Description = 'Displays the namespace of the wiki page.', TemplateText = '##Namespace
', PageId = (SELECT Id FROM Page WHERE Name = '65' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'NamespaceGlossary', 'Standard Function', (SELECT Id FROM Page WHERE Name = '64' LIMIT 1), 'Create a glossary from of pages in specified namespaces.', '##NamespaceGlossary("Wiki Help, Other Help", 10)
'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Standard Function', Description = 'Create a glossary from of pages in specified namespaces.', TemplateText = '##NamespaceGlossary("Wiki Help, Other Help", 10)
', PageId = (SELECT Id FROM Page WHERE Name = '64' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'NamespaceList', 'Standard Function', (SELECT Id FROM Page WHERE Name = '63' LIMIT 1), 'Create a list of pages from specified namespaces.', '##NamespaceList("Wiki Help, Other Help", 20)
'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Standard Function', Description = 'Create a list of pages from specified namespaces.', TemplateText = '##NamespaceList("Wiki Help, Other Help", 20)
', PageId = (SELECT Id FROM Page WHERE Name = '63' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'Navigation', 'Standard Function', (SELECT Id FROM Page WHERE Name = '47' LIMIT 1), 'Diaplys the URL friendly navigation name of a page.', '##Navigation
'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Standard Function', Description = 'Diaplys the URL friendly navigation name of a page.', TemplateText = '##Navigation
', PageId = (SELECT Id FROM Page WHERE Name = '47' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'NoCache', 'Instruction Function', (SELECT Id FROM Page WHERE Name = '72' LIMIT 1), 'Processing instruction indicating that the page should never be cached.', '@@NoCache
'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Instruction Function', Description = 'Processing instruction indicating that the page should never be cached.', TemplateText = '@@NoCache
', PageId = (SELECT Id FROM Page WHERE Name = '72' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'Order', 'Scope Function', (SELECT Id FROM Page WHERE Name = '73' LIMIT 1), 'Orders the resulting lines of a block of wiki markup.', '{{Order
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
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Scope Function', Description = 'Orders the resulting lines of a block of wiki markup.', TemplateText = '{{Order
This
Is
A
List
Of
Lines
To
Sort
}}
', PageId = (SELECT Id FROM Page WHERE Name = '73' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'Protect', 'Instruction Function', (SELECT Id FROM Page WHERE Name = '53' LIMIT 1), 'Processing instruction indicating the page is protected from unauthorized modification.', '@@Protect(false)
'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Instruction Function', Description = 'Processing instruction indicating the page is protected from unauthorized modification.', TemplateText = '@@Protect(false)
', PageId = (SELECT Id FROM Page WHERE Name = '53' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'RecentlyModified', 'Standard Function', (SELECT Id FROM Page WHERE Name = '33' LIMIT 1), 'Display a list of recently modified wiki pages.', '##RecentlyModified(10, Full, True)
'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Standard Function', Description = 'Display a list of recently modified wiki pages.', TemplateText = '##RecentlyModified(10, Full, True)
', PageId = (SELECT Id FROM Page WHERE Name = '33' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'Related', 'Standard Function', (SELECT Id FROM Page WHERE Name = '59' LIMIT 1), 'Create a list of related pages based on incoming links.', '##Related(List, 20, False)
'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Standard Function', Description = 'Create a list of related pages based on incoming links.', TemplateText = '##Related(List, 20, False)
', PageId = (SELECT Id FROM Page WHERE Name = '59' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'Review', 'Instruction Function', (SELECT Id FROM Page WHERE Name = '55' LIMIT 1), 'Processing instruction indicating the page needs to be reviewed.', '@@Review
'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Instruction Function', Description = 'Processing instruction indicating the page needs to be reviewed.', TemplateText = '@@Review
', PageId = (SELECT Id FROM Page WHERE Name = '55' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'Revisions', 'Standard Function', (SELECT Id FROM Page WHERE Name = '43' LIMIT 1), 'Displays the revision history of a page.', '##Revisions(Full, 10, true)
'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Standard Function', Description = 'Displays the revision history of a page.', TemplateText = '##Revisions(Full, 10, true)
', PageId = (SELECT Id FROM Page WHERE Name = '43' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'SearchCloud', 'Standard Function', (SELECT Id FROM Page WHERE Name = '31' LIMIT 1), 'Create a cloud of pages from a search of wiki body contents.', '##SearchCloud("Wiki help color", 10)
'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Standard Function', Description = 'Create a cloud of pages from a search of wiki body contents.', TemplateText = '##SearchCloud("Wiki help color", 10)
', PageId = (SELECT Id FROM Page WHERE Name = '31' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'SearchList', 'Standard Function', (SELECT Id FROM Page WHERE Name = '29' LIMIT 1), 'Create  a list from a search of wiki body contents.', '##SearchList("Wiki help color")
'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Standard Function', Description = 'Create  a list from a search of wiki body contents.', TemplateText = '##SearchList("Wiki help color")
', PageId = (SELECT Id FROM Page WHERE Name = '29' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'Seq', 'Standard Function', (SELECT Id FROM Page WHERE Name = '62' LIMIT 1), 'Automatically create a numbered sequence on a page.', '**##Seq** - This is my first point.
You can have content between the sequences.
**##Seq** - This is my second point.
You can have additional content between the sequences.
**##Seq** - This is my third point.
'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Standard Function', Description = 'Automatically create a numbered sequence on a page.', TemplateText = '**##Seq** - This is my first point.
You can have content between the sequences.
**##Seq** - This is my second point.
You can have additional content between the sequences.
**##Seq** - This is my third point.
', PageId = (SELECT Id FROM Page WHERE Name = '62' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'Set', 'Standard Function', (SELECT Id FROM Page WHERE Name = '25' LIMIT 1), 'Set wiki page variable value.', '${variableName = value}

${variableName}
'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Standard Function', Description = 'Set wiki page variable value.', TemplateText = '${variableName = value}

${variableName}
', PageId = (SELECT Id FROM Page WHERE Name = '25' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'Similar', 'Standard Function', (SELECT Id FROM Page WHERE Name = '60' LIMIT 1), 'Create a list of similar pages based on tag similarities.', '##Similar(20)
'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Standard Function', Description = 'Create a list of similar pages based on tag similarities.', TemplateText = '##Similar(20)
', PageId = (SELECT Id FROM Page WHERE Name = '60' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'SiteName', 'Standard Function', (SELECT Id FROM Page WHERE Name = '103' LIMIT 1), 'Displays the name of the configured site.', '##SiteName
'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Standard Function', Description = 'Displays the name of the configured site.', TemplateText = '##SiteName
', PageId = (SELECT Id FROM Page WHERE Name = '103' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'Snippet', 'Standard Function', (SELECT Id FROM Page WHERE Name = '76' LIMIT 1), 'Injects the value of a previously defined snippet variable.', '{{DefineSnippet(MyBlock)
This is the content of the snippet variable.
}}

##Snippet(MyBlock)
##Snippet(MyBlock)
##Snippet(MyBlock)
'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Standard Function', Description = 'Injects the value of a previously defined snippet variable.', TemplateText = '{{DefineSnippet(MyBlock)
This is the content of the snippet variable.
}}

##Snippet(MyBlock)
##Snippet(MyBlock)
##Snippet(MyBlock)
', PageId = (SELECT Id FROM Page WHERE Name = '76' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'StripedTable', 'Scope Function', (SELECT Id FROM Page WHERE Name = '74' LIMIT 1), 'The StripedTable scope function allows you to create tables with columns and alternating colored rows.', '{{StripedTable
Column #1 || Column #2 || Column #3 || Column #4
Row #1 || Cell #1 || Cell #2 || Cell #3
Row #2 || Cell #1 || Cell #2 || Cell #3
Row #3 || Cell #1 || Cell #2 || Cell #3
Row #4 || Cell #1 || Cell #2 || Cell #3
}}
'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Scope Function', Description = 'The StripedTable scope function allows you to create tables with columns and alternating colored rows.', TemplateText = '{{StripedTable
Column #1 || Column #2 || Column #3 || Column #4
Row #1 || Cell #1 || Cell #2 || Cell #3
Row #2 || Cell #1 || Cell #2 || Cell #3
Row #3 || Cell #1 || Cell #2 || Cell #3
Row #4 || Cell #1 || Cell #2 || Cell #3
}}
', PageId = (SELECT Id FROM Page WHERE Name = '74' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'Table', 'Scope Function', (SELECT Id FROM Page WHERE Name = '66' LIMIT 1), 'The Table scope function allows you to create tables with rows and columns.', '{{Table
Column #1 || Column #2 || Column #3 || Column #4
Row #1 || Cell #1 || Cell #2 || Cell #3
Row #2 || Cell #1 || Cell #2 || Cell #3
Row #3 || Cell #1 || Cell #2 || Cell #3
Row #4 || Cell #1 || Cell #2 || Cell #3
}}
'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Scope Function', Description = 'The Table scope function allows you to create tables with rows and columns.', TemplateText = '{{Table
Column #1 || Column #2 || Column #3 || Column #4
Row #1 || Cell #1 || Cell #2 || Cell #3
Row #2 || Cell #1 || Cell #2 || Cell #3
Row #3 || Cell #1 || Cell #2 || Cell #3
Row #4 || Cell #1 || Cell #2 || Cell #3
}}
', PageId = (SELECT Id FROM Page WHERE Name = '66' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'TagCloud', 'Standard Function', (SELECT Id FROM Page WHERE Name = '35' LIMIT 1), 'Create a tag cloud from a seed tag.', '##TagCloud("Help", 100)
'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Standard Function', Description = 'Create a tag cloud from a seed tag.', TemplateText = '##TagCloud("Help", 100)
', PageId = (SELECT Id FROM Page WHERE Name = '35' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'TagGlossary', 'Standard Function', (SELECT Id FROM Page WHERE Name = '32' LIMIT 1), 'Create a glossary from a search of specified tags.', '##TagGlossary("Official-Help, help", 5)
'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Standard Function', Description = 'Create a glossary from a search of specified tags.', TemplateText = '##TagGlossary("Official-Help, help", 5)
', PageId = (SELECT Id FROM Page WHERE Name = '32' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'TagList', 'Standard Function', (SELECT Id FROM Page WHERE Name = '30' LIMIT 1), 'Create a list of pages from a search of page tags.', '##TagList("Official-Help, help")
'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Standard Function', Description = 'Create a list of pages from a search of page tags.', TemplateText = '##TagList("Official-Help, help")
', PageId = (SELECT Id FROM Page WHERE Name = '30' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'Tags', 'Instruction Function', (SELECT Id FROM Page WHERE Name = '28' LIMIT 1), 'Associate tags with a page.', '##Tags(These, are, the, tags, we, want, to, add)
'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Instruction Function', Description = 'Associate tags with a page.', TemplateText = '##Tags(These, are, the, tags, we, want, to, add)
', PageId = (SELECT Id FROM Page WHERE Name = '28' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'Tags', 'Standard Function', (SELECT Id FROM Page WHERE Name = '38' LIMIT 1), 'Display the tags associated with the page.', '##Tags
'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Standard Function', Description = 'Display the tags associated with the page.', TemplateText = '##Tags
', PageId = (SELECT Id FROM Page WHERE Name = '38' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'Template', 'Instruction Function', (SELECT Id FROM Page WHERE Name = '54' LIMIT 1), 'Processing instruction indicating the page is a template.', '@@Template
'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Instruction Function', Description = 'Processing instruction indicating the page is a template.', TemplateText = '@@Template
', PageId = (SELECT Id FROM Page WHERE Name = '54' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'TextGlossary', 'Standard Function', (SELECT Id FROM Page WHERE Name = '34' LIMIT 1), 'Create a glossary from a search of wiki body contens.', '##TextGlossary("Wiki help", 10, Full)
'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Standard Function', Description = 'Create a glossary from a search of wiki body contens.', TemplateText = '##TextGlossary("Wiki help", 10, Full)
', PageId = (SELECT Id FROM Page WHERE Name = '34' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'Title', 'Instruction Function', (SELECT Id FROM Page WHERE Name = '102' LIMIT 1), 'Sets the title of the page for the browser (HTML head title).', '##Title("My Wiki Site")
'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Instruction Function', Description = 'Sets the title of the page for the browser (HTML head title).', TemplateText = '##Title("My Wiki Site")
', PageId = (SELECT Id FROM Page WHERE Name = '102' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'Title', 'Standard Function', (SELECT Id FROM Page WHERE Name = '46' LIMIT 1), 'Displays the title of the wiki page in title format.', '##Title
'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Standard Function', Description = 'Displays the title of the wiki page in title format.', TemplateText = '##Title
', PageId = (SELECT Id FROM Page WHERE Name = '46' LIMIT 1);
INSERT INTO FeatureTemplate(Name, Type, PageId, Description, TemplateText)
SELECT 'TOC', 'Standard Function', (SELECT Id FROM Page WHERE Name = '45' LIMIT 1), 'Generate a table-of-contents for a page from the headings.', '##TOC
'
ON CONFLICT(Name, Type) DO UPDATE SET Type = 'Standard Function', Description = 'Generate a table-of-contents for a page from the headings.', TemplateText = '##TOC
', PageId = (SELECT Id FROM Page WHERE Name = '45' LIMIT 1);
