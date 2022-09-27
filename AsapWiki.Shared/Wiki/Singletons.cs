using AsapWiki.Shared.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsapWiki.Shared.Wiki
{
    public static class Singletons
    {
        private static MethodPrototypes _methodPrototypes;
        public static MethodPrototypes MethodPrototypes
        {
            get
            {
                if (_methodPrototypes == null)
                {
                    _methodPrototypes = new MethodPrototypes();
                    _methodPrototypes.Add("PanelScope: <string>[boxType(bullets,bullets-ordered,alert,alert-default,alert-info,alert-danger,alert-warning,alert-success,jumbotron,block,block-default,block-primary,block-success,block-success,block-info,block-warning,block-danger,panel,panel-default,panel-primary,panel-success,panel-info,panel-warning,panel-danger)] | <string>{title}=''");
                    _methodPrototypes.Add("Tag: <string:infinite>[tags]");
                    _methodPrototypes.Add("TextList: <string:infinite>[tags] | <int>{Top}='1000' | <string>{view(List,Full)}='Full'");
                    _methodPrototypes.Add("TagList: <string:infinite>[tags] | <int>{Top}='1000' | <string>{view(List,Full)}='Full'");
                    _methodPrototypes.Add("SearchCloud: <string:infinite>[tokens] | <int>{Top}='1000'");
                    _methodPrototypes.Add("TagGlossary: <string:infinite>[tags] | <int>{Top}='1000' | <string>{view(List,Full)}='Full'");
                    _methodPrototypes.Add("RecentlyModified: <int>{Top}='1000' | <string>{view(List,Full)}='Full'");
                    _methodPrototypes.Add("TextGlossary: <string:infinite>[tokens] | <int>{Top}='1000' | <string>{view(List,Full)}='Full'");
                    _methodPrototypes.Add("TagCloud: <string:infinite>[tags] | <int>{Top}='1000'");
                    _methodPrototypes.Add("Image: <string>[name] | <int>{scale}='100'");
                    _methodPrototypes.Add("File: <string>[name] | <bool>{showSize}='false'");
                    _methodPrototypes.Add("Related: <int>{Top}='1000' | <string>{view(List,Flat,Full)}='Full'");
                    _methodPrototypes.Add("Tags: <string>{view(Flat,list)}='list'");
                    _methodPrototypes.Add("Include: <string>[pageName]");
                    _methodPrototypes.Add("BR: <int>{Count}='1'");
                    _methodPrototypes.Add("NL: <int>{Count}='1'");
                    _methodPrototypes.Add("NewLine: <int>{Count}='1'");
                    _methodPrototypes.Add("TOC:");
                    _methodPrototypes.Add("Title:");
                    _methodPrototypes.Add("Navigation:");
                    _methodPrototypes.Add("Name:");
                    _methodPrototypes.Add("Created:");
                    _methodPrototypes.Add("LastModified:");
                    _methodPrototypes.Add("Files:");
                }

                return _methodPrototypes;
            }
        }
    }
}
