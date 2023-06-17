using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoViewer.Core.Utils;

public static class SvgUtil
{

    public static string EmbedInHtml(string svg)
    {
        return $$"""
            <html>
                <head>
                    <style>
                        body {
                            margin: 0px;\n" +
                        }  
                            
                        ::-webkit-scrollbar {
                            display: none;
                        }

                        svg {
                            max-width: 100%;
                            max-height: 100%;
                            position: absolute;
                            top: 50%;
                            left: 50%;
                            translate: -50% -50%;
                        }
                    </style>
                </head>
                <body>
                    {{svg}}
                </body>
            </html>
            """;
    }

}
