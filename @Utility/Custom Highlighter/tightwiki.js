/*
Language: TightWiki
Description: Highlighter for TightWiki markup.
Authors: Josh Patterson <Josh@NTDLS.com>
Website: https://networkdls.com/
Category: common
*/

/** @type LanguageFn */
export default function(hljs) {
  const regex = hljs.regex;
  /**
   * Character Literal
   * Either a single character ("a"C) or an escaped double quote (""""C).
   */
  const CHARACTER = {
    className: 'string',
    begin: /"(""|[^/n])"C\b/
  };

  const STRING = {
    className: 'string',
    begin: /"/,
    end: /"/,
    illegal: /\n/,
    contains: [
      {
        // double quote escape
        begin: /""/ }
    ]
  };

  /** Date Literals consist of a date, a time, or both separated by whitespace, surrounded by # */
  const MM_DD_YYYY = /\d{1,2}\/\d{1,2}\/\d{4}/;
  const YYYY_MM_DD = /\d{4}-\d{1,2}-\d{1,2}/;
  const TIME_12H = /(\d|1[012])(:\d+){0,2} *(AM|PM)/;
  const TIME_24H = /\d{1,2}(:\d{1,2}){1,2}/;
  const DATE = {
    className: 'literal',
    variants: [
      {
        // #YYYY-MM-DD# (ISO-Date) or #M/D/YYYY# (US-Date)
        begin: regex.concat(/# */, regex.either(YYYY_MM_DD, MM_DD_YYYY), / *#/) },
      {
        // #H:mm[:ss]# (24h Time)
        begin: regex.concat(/# */, TIME_24H, / *#/) },
      {
        // #h[:mm[:ss]] A# (12h Time)
        begin: regex.concat(/# */, TIME_12H, / *#/) },
      {
        // date plus time
        begin: regex.concat(
          /# */,
          regex.either(YYYY_MM_DD, MM_DD_YYYY),
          / +/,
          regex.either(TIME_12H, TIME_24H),
          / *#/
        ) }
    ]
  };

  const NUMBER = {
    className: 'number',
    relevance: 0,
    variants: [
      {
        // Float
        begin: /\b\d[\d_]*((\.[\d_]+(E[+-]?[\d_]+)?)|(E[+-]?[\d_]+))[RFD@!#]?/ },
      {
        // Integer (base 10)
        begin: /\b\d[\d_]*((U?[SIL])|[%&])?/ },
      {
        // Integer (base 16)
        begin: /&H[\dA-F_]+((U?[SIL])|[%&])?/ },
      {
        // Integer (base 8)
        begin: /&O[0-7_]+((U?[SIL])|[%&])?/ },
      {
        // Integer (base 2)
        begin: /&B[01_]+((U?[SIL])|[%&])?/ }
    ]
  };

  const LABEL = {
    className: 'label',
    begin: /^\w+:/
  };

  const DOC_COMMENT = hljs.COMMENT(/'''/, /$/, { contains: [
    {
      className: 'doctag',
      begin: /<\/?/,
      end: />/
    }
  ] });

  const COMMENT = hljs.COMMENT(null, /$/, { variants: [
    { begin: /'/ },
    {
      // TODO: Use multi-class for leading spaces
      begin: /([\t ]|^)REM(?=\s)/ }
  ] });

  const DIRECTIVES = {
    className: 'meta',
    // TODO: Use multi-class for indentation once available
    begin: /[\t ]*#(const|disable|else|elseif|enable|end|externalsource|if|region)\b/,
    end: /$/,
    keywords: { keyword:
        'const disable else elseif enable end externalsource if region then' },
    contains: [ COMMENT ]
  };

  return {
    name: 'TightWiki',
    aliases: [ 'wiki' ],
    case_insensitive: true,
    classNameAliases: { label: 'symbol' },
    keywords: {
      keyword:
        'NamespaceGlossary NamespaceList Namespace Code Bullets Table StripedTable Jumbotron Callout Background Foreground Alert Card Collapse Tag SearchList TagList SearchCloud TagGlossary RecentlyModified TextGlossary TagCloud Image File Related Tags EditLink Include Inject BR NL HR NewLine History Attachments TOC Title Navigation Name Created LastModified AppVersion Deprecate Protect Template Review Include HideFooterComments NoCache Draft Order definesnippet snippet',
      built_in:
        'infinite',
      type:
        'boolean integer string decimal',
      literal: 'true false null'
    },
    illegal:
      '//|\\{|\\}|endif|gosub|variant|wend|^\\$ ' /* reserved deprecated keywords */,
    contains: [
      CHARACTER,
      STRING,
      DATE,
      NUMBER,
      LABEL,
      DOC_COMMENT,
      COMMENT,
      DIRECTIVES
    ]
  };
}
