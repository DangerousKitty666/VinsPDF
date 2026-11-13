using System;
using System.Runtime.InteropServices;
namespace VinsPDF {
internal static partial class Native {
 [DllImport("pdfium.dll",CharSet=CharSet.Ansi)]internal static extern int FPDFPageObjMark_SetStringParam(IntPtr document,IntPtr obj,IntPtr mark,string key,string value);
 [DllImport("pdfium.dll",CharSet=CharSet.Ansi)]internal static extern int FPDFPageObjMark_GetParamStringValue(IntPtr mark,string key,IntPtr buffer,uint length,out uint required);
 [DllImport("pdfium.dll")]internal static extern int FPDFText_CountChars(IntPtr page);
 [DllImport("pdfium.dll")]internal static extern uint FPDFText_GetUnicode(IntPtr page,int index);
 [DllImport("pdfium.dll")]internal static extern IntPtr FPDFText_GetTextObject(IntPtr page,int index);
 [DllImport("pdfium.dll")]internal static extern int FPDFPageObj_CountMarks(IntPtr obj);
 [DllImport("pdfium.dll")]internal static extern IntPtr FPDFPageObj_GetMark(IntPtr obj,uint index);
 [DllImport("pdfium.dll",CharSet=CharSet.Ansi)]internal static extern IntPtr FPDFPageObj_AddMark(IntPtr obj,string name);
 [DllImport("pdfium.dll")]internal static extern int FPDFPageObjMark_GetName(IntPtr mark,IntPtr buffer,uint length,out uint required);
}
}
