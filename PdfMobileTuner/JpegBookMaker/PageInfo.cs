﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using PdfLib;

namespace JpegBookMaker
{
    public class PageInfo
    {
        public string Path { get; private set; }
        public PdfObject Page { get; private set; }
        public Rectangle Bounds;
        public int Level, Contrast;
        public bool IsGrayScale;

        public PageInfo()
        {
        }

        public PageInfo(string path)
        {
            Path = path;
        }

        public PageInfo(PdfObject page)
        {
            Page = page;
        }

        public Bitmap GetBitmap(string imgPath)
        {
            if (Path != null)
                return new Bitmap(System.IO.Path.Combine(imgPath, Path));
            else if (Page != null)
                return new Bitmap(GetImage(Page).GetStream());
            else
                return null;
        }

        public static PdfObject GetImage(PdfObject page)
        {
            var rsrc = page.GetObject("/Resources");
            if (rsrc == null) return null;
            var xobj = rsrc.GetObject("/XObject");
            if (xobj == null) return null;

            foreach (var key in xobj.Keys)
            {
                var obj = xobj.GetObject(key);
                if (obj != null
                    && obj.GetText("/Subtype") == "/Image"
                    && obj.GetText("/Filter") == "/DCTDecode")
                {
                    return obj;
                }
            }
            return null;
        }
    }
}
