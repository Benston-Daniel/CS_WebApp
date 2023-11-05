using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Speech.Synthesis;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Utils;
using iText.Layout;

namespace YourNamespace.Controllers
{
    [Route("api/pdf-to-speech")]
    [ApiController]
    public class TextToSpeechController : ControllerBase
    {
        [HttpGet("/")]
        public IActionResult RenderIndexHtml()
        {
            string contentRootPath = Directory.GetCurrentDirectory();
            string filePath = Path.Combine(contentRootPath, "wwwroot", "index.html");

            if (System.IO.File.Exists(filePath))
            {
                var fileContent = System.IO.File.ReadAllText(filePath);
                return Content(fileContent, "text/html");
            }

            return NotFound();
        }

[HttpPost("synthesize")]
public IActionResult SynthesizePdf()
{
    var audioFilePaths = new List<string>();
    List<MemoryStream> pdfStreams = new List<MemoryStream>();
    string mergedTextFilePath = Path.Combine(Directory.GetCurrentDirectory(), "MergedText.txt");

    try
    {
        using (StreamWriter sw = new StreamWriter(mergedTextFilePath))
        {
            for (int i = 0; i < Request.Form.Files.Count; i++)
            {
                var pdf = Request.Form.Files[i];
                if (pdf.ContentType != "application/pdf")
                {
                    return StatusCode(415, "Unsupported Media Type");
                }

                using (MemoryStream ms = new MemoryStream())
                {
                    pdf.CopyTo(ms);
                    ms.Position = 0;
                    pdfStreams.Add(ms);

                    string pdfText = ReadPdfText(ms);

                    sw.WriteLine(pdfText);
                }
            }
        }

        // Merge PDF contents into a single text file

        foreach (var stream in pdfStreams)
        {
            stream.Position = 0;
        }

        using (PdfDocument pdfDocument = new PdfDocument(new PdfWriter(new FileStream(mergedTextFilePath, FileMode.Create, FileAccess.Write))))
        {
            PdfMerger merger = new PdfMerger(pdfDocument);
            foreach (var stream in pdfStreams)
            {
                PdfDocument tempDoc = new PdfDocument(new PdfReader(stream));
                merger.Merge(tempDoc, 1, tempDoc.GetNumberOfPages());
            }
        }

        // Synthesize speech from the merged text

        string audioFilePath = Path.Combine(Directory.GetCurrentDirectory(), "output.wav");
        SpeechSynthesizer synthesizer = new SpeechSynthesizer();
        synthesizer.SetOutputToWaveFile(audioFilePath);
        // Set speech synthesis configurations: voice, language, volume, etc.
        // Example settings:
        synthesizer.SelectVoiceByHints(VoiceGender.Female, VoiceAge.Adult, 0, new System.Globalization.CultureInfo("en-US"));
        synthesizer.Volume = 100;
        synthesizer.Rate = 0;

        string mergedText = System.IO.File.ReadAllText(mergedTextFilePath);

        synthesizer.Speak(mergedText);
        synthesizer.SetOutputToNull();

        audioFilePaths.Add(audioFilePath);
    }
    catch (Exception ex)
    {
        return BadRequest("Error synthesizing PDF: " + ex.Message);
    }

    // Return the list of audio file paths and the merged text file path
    return Ok(new { AudioFilePaths = audioFilePaths, MergedTextFilePath = mergedTextFilePath });
}
        private string ReadPdfText(Stream stream)
        {
            using (StreamReader reader = new StreamReader(stream))
            {
                iText.Kernel.Pdf.PdfReader pdfReader = new iText.Kernel.Pdf.PdfReader(reader.BaseStream);
                iText.Kernel.Pdf.PdfDocument pdfDocument = new iText.Kernel.Pdf.PdfDocument(pdfReader);
                StringWriter output = new StringWriter();

                for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
                {
                    string pageText = iText.Kernel.Pdf.Canvas.Parser.PdfTextExtractor.GetTextFromPage(pdfDocument.GetPage(i));
                    output.WriteLine(pageText);
                }
                pdfDocument.Close();
                pdfReader.Close();
                return output.ToString();
            }

    }
}
}
