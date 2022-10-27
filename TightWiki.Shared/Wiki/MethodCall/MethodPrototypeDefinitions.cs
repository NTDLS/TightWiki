namespace TightWiki.Shared.Wiki.MethodCall
{
    public static class MethodPrototypeDefinitions
    {
        private static MethodPrototypeCollection _methodPrototypes;

        public static MethodPrototype Get(string methodName)
        {
            return Collection.Get(methodName);
        }

        public static MethodPrototypeCollection Collection
        {
            get
            {
                if (_methodPrototypes == null)
                {
                    _methodPrototypes = new MethodPrototypeCollection();

                    //Scope functions:
                    _methodPrototypes.Add("$$Code: <string>{language(auto,cpp,lua,graphql,swift,r,yaml,kotlin,scss,shell,vbnet,json,objectivec,perl,diff,wasm,php,xml,bash,csharp,css,go,ini,javascript,less,makefile,markdown,plaintext,python,python-repl,ruby,rust,sql,typescript)}='auto'");
                    _methodPrototypes.Add("$$Bullets: <string>{type(unordered,ordered)}='unordered' | <string>{title}=''");
                    _methodPrototypes.Add("$$Jumbotron:");
                    _methodPrototypes.Add("$$Callout: <string>{style(default,primary,secondary,success,info,warning,danger)}='default' | <string>{title}=''");
                    _methodPrototypes.Add("$$Background: <string>{style(default,primary,secondary,light,dark,success,info,warning,danger,muted)}='default'");
                    _methodPrototypes.Add("$$Foreground: <string>{style(default,primary,secondary,light,dark,success,info,warning,danger,muted)}='default'");
                    _methodPrototypes.Add("$$Alert: <string>{style(default,primary,secondary,light,dark,success,info,warning,danger)}='default' | <string>{title}=''");
                    _methodPrototypes.Add("$$Card: <string>{style(default,primary,secondary,light,dark,success,info,warning,danger)}='default' | <string>{title}=''");
                    _methodPrototypes.Add("$$Collpase: <string>{linkText}='Show'");

                    //Standard functions:
                    _methodPrototypes.Add("##Tag: <string:infinite>[tags]");
                    _methodPrototypes.Add("##SearchList: <string:infinite>[tokens] | <string>{view(List,Full)}='Full' | <integer>{pageSize}='5' | <bool>{pageSelector}='true' | <bool>{allowFuzzyMatching}='false'");
                    _methodPrototypes.Add("##TagList: <string:infinite>[tags] | <integer>{Top}='1000' | <string>{view(List,Full)}='Full'");
                    _methodPrototypes.Add("##SearchCloud: <string:infinite>[tokens] | <integer>{Top}='1000'");
                    _methodPrototypes.Add("##TagGlossary: <string:infinite>[tags] | <integer>{Top}='1000' | <string>{view(List,Full)}='Full'");
                    _methodPrototypes.Add("##RecentlyModified: <integer>{Top}='1000' | <string>{view(List,Full)}='Full'");
                    _methodPrototypes.Add("##TextGlossary: <string:infinite>[tokens] | <integer>{Top}='1000' | <string>{view(List,Full)}='Full'");
                    _methodPrototypes.Add("##TagCloud: <string>[tag] | <integer>{Top}='1000'");
                    _methodPrototypes.Add("##Image: <string>[name] | <integer>{scale}='100' | <string>{altText}=''");
                    _methodPrototypes.Add("##File: <string>[name] | <string>[linkText] | <bool>{showSize}='false'");
                    _methodPrototypes.Add("##Related: <integer>{Top}='1000' | <string>{view(List,Flat,Full)}='Full'");
                    _methodPrototypes.Add("##Tags: <string>{view(Flat,List)}='List'");
                    _methodPrototypes.Add("##EditLink: <string>{linkText}='edit'");
                    _methodPrototypes.Add("##Inject: <string>[pageName]");
                    _methodPrototypes.Add("##BR: <integer>{Count}='1'");
                    _methodPrototypes.Add("##NL: <integer>{Count}='1'");
                    _methodPrototypes.Add("##HR: <integer>{Height}='1'");
                    _methodPrototypes.Add("##NewLine: <integer>{Count}='1'");
                    _methodPrototypes.Add("##History:<string>{view(Full,List)}='Full' | <integer>{pageSize}='5' | <bool>{pageSelector}='true' | <string>{pageName}=''");
                    _methodPrototypes.Add("##Attachments:<string>{view(Full,List)}='Full' | <integer>{pageSize}='5' | <bool>{pageSelector}='true' | <string>{pageName}=''");
                    _methodPrototypes.Add("##TOC:<bool>{alphabetized}='false'");
                    _methodPrototypes.Add("##Title:");
                    _methodPrototypes.Add("##Navigation:");
                    _methodPrototypes.Add("##Name:");
                    _methodPrototypes.Add("##Created:");
                    _methodPrototypes.Add("##LastModified:");
                    _methodPrototypes.Add("##AppVersion:");

                    //Processing instructions:
                    _methodPrototypes.Add("@@Deprecate:");
                    _methodPrototypes.Add("@@Protect:<bool>{isSilent}='false'");
                    _methodPrototypes.Add("@@Template:");
                    _methodPrototypes.Add("@@Review:");
                    _methodPrototypes.Add("@@Include:");
                    _methodPrototypes.Add("@@Draft:");
                }

                return _methodPrototypes;
            }
        }
    }
}
