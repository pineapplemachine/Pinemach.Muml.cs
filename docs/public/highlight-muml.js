/**
 * Muml syntax highlighting definition for highlight.js.
 */

const mumlCommon = [
    {
        className: "comment",
        variants: [
            hljs.END_SAME_AS_BEGIN({
                begin: /#{3,}/,
                end: /#{3,}/,
            }),
            {
                begin: /#\[/,
                end: /#\]/,
                contains: ['self'],
            },
            {
                begin: /#[^\n]*/,
            },
        ]
    },
    {
        className: "string",
        variants: [
            hljs.END_SAME_AS_BEGIN({
                begin: /('{3,}|"{3,})/,
                end: /('{3,}|"{3,})/,
                contains: [hljs.BACKSLASH_ESCAPE],
            }),
            hljs.END_SAME_AS_BEGIN({
                begin: /`{3,}/,
                end: /`{3,}/,
            }),
            {
                begin: '`',
                end: '`',
                multi: true,
                contains: [{begin: '``'}],
            },
            {
                begin: '\'',
                end: '\'',
                contains: [hljs.BACKSLASH_ESCAPE],
            },
            {
                begin: '"',
                end: '"',
                contains: [hljs.BACKSLASH_ESCAPE],
            },
            {
                begin: /\|($|[ \t].*)/,
            }
        ]
    },
    {
        className: "meta",
        begin: /\|[|>;^=][$+*\-]?\.*/
    },
    {
        className: "punctuation",
        begin: /[(){};&]/
    },
];

hljs.registerLanguage('muml', () => ({
    case_insensitive: true,
    contains: [
        {
            className: "punctuation",
            begin: /\[/,
            end: /\]/,
            contains: [
                ...mumlCommon,
                {
                    className: 'attr',
                    begin: /[^ \t\r\n`''"()\[\]{}|&;,#=]+/,
                },
                {
                    className: 'attr',
                    begin: /=/,
                },
            ],
        },
        {
            className: "punctuation",
            begin: /[\]=]/,
        },
        ...mumlCommon,
    ]
}));
