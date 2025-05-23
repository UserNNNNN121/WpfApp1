
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Windows;
using PdfSharp.Pdf;
using PdfSharp.Drawing;
using PdfSharp.Pdf.IO;
using PdfSharp.Pdf.AcroForms;
using PdfSharp.Pdf.Advanced;
using System.Collections;
using System.Drawing;
namespace WpfApp1
{
    public class CertificateGenerator
    {
        private static readonly string connectionString =
            $"Data Source={Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "educatingsystem.sl3")};Version=3;";

        public byte[] GenerateCertificate(
            string userName,
            string courseTitle,
            string organisationName,
            byte[] templatePdf,
            byte[] sealImage,
            byte[] signatureImage,
            string certificateNumber,
            DateTime completionDate)
        {
            if (templatePdf == null || templatePdf.Length == 0)
            {
                throw new ArgumentException("PDF template is required");
            }

            try
            {

                using (var templateStream = new MemoryStream(templatePdf))
                using (var outputStream = new MemoryStream())
                {
                    var document = PdfReader.Open(templateStream, PdfDocumentOpenMode.Modify);
                    document.AcroForm.Elements.SetBoolean("/NeedAppearances", true);

                    if (document.AcroForm != null && document.AcroForm.Fields != null)
                    {
                        FillFormFields(document, userName, courseTitle, organisationName,
                                     certificateNumber, completionDate);
                    }
                    else
                    {
                       
                        FillManually(document, userName, courseTitle, organisationName,
                                     certificateNumber, completionDate);
                    }

                    AddImages(document, sealImage, signatureImage);

                    MakeFieldsReadOnly(document);

                    document.Save(outputStream);
                    return outputStream.ToArray();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating certificate: {ex.Message}");
                throw;
            }
        }
        private void FillFormFields(PdfDocument document,
                                    string userName,
                                    string courseTitle,
                                    string organisationName,
                                    string certificateNumber,
                                    DateTime completionDate)
        {
            var fields = document.AcroForm.Fields;

            document.AcroForm.Elements.SetBoolean("/NeedAppearances", true);

            var fieldValues = new Dictionary<string, string>
    {
        {"Text1", userName},
        {"Text3", courseTitle},
        {"Text10", organisationName},
        {"Text5", organisationName},
        {"Text6", certificateNumber},
        {"Text7", completionDate.ToString("dd.MM.yyyy")},
        {"Text4", completionDate.ToString("dd.MM.yyyy")}
    };

            foreach (var field in fieldValues)
            {
                if (fields[field.Key] is PdfTextField textField)
                {
                    textField.Value = new PdfString(field.Value);

                    SetReadOnlyFlag(textField);
                }
            }
        }
        private void SetReadOnlyFlag(PdfTextField field)
        {
            const int ReadOnlyFlag = 1; 

            if (field.Elements.ContainsKey("/Ff"))
            {
                int flags = field.Elements.GetInteger("/Ff");
                flags |= ReadOnlyFlag; 
                field.Elements.SetInteger("/Ff", flags);
            }
            else
            {
                field.Elements.SetInteger("/Ff", ReadOnlyFlag);
            }
        }


        private void FillManually(PdfDocument document,
                        string userName,
                        string courseTitle,
                        string organisationName,
                        string certificateNumber,
                        DateTime completionDate)
        {
            var page = document.Pages[0];
            var gfx = XGraphics.FromPdfPage(page);
            var font = new XFont("Arial", 20);
            var regularFont = new XFont("Arial", 18);


            gfx.DrawString(userName, font, XBrushes.Black, new XPoint(150, 250)); 
            gfx.DrawString(courseTitle, regularFont, XBrushes.Black, new XPoint(150, 280)); 
            gfx.DrawString(organisationName, regularFont, XBrushes.Black, new XPoint(150, 310)); 
            gfx.DrawString(certificateNumber, regularFont, XBrushes.Black, new XPoint(150, 340)); 
            gfx.DrawString(completionDate.ToString("dd.MM.yyyy"), regularFont, XBrushes.Black, new XPoint(150, 370)); 
        }


        private void AddImages(PdfDocument document, byte[] sealImage, byte[] signatureImage)
        {
            var page = document.Pages[0];
            var gfx = XGraphics.FromPdfPage(page);

            var fields = document.AcroForm?.Fields;

            // Подпись -> Text8
            if (signatureImage != null && fields["Text8"] is PdfTextField signatureField)
            {
                var rect = signatureField.Elements.GetRectangle("/Rect");
                double x = rect.X1;
                double y = page.Height - rect.Y2;
                double width = rect.Width;
                double height = rect.Height;

                signatureField.Value = new PdfString("");
                signatureField.ReadOnly = true;

                using (var signatureStream = new MemoryStream(signatureImage))
                {
                    var signature = XImage.FromStream(signatureStream);
                    gfx.DrawImage(signature, x, y, width, height);
                }

                // Удаление поля Text8
                RemoveFieldManually(document, "Text8");
            }

            if (sealImage != null && fields["Text9"] is PdfTextField sealField)
            {
                var rect = sealField.Elements.GetRectangle("/Rect");
                double x = rect.X1;
                double y = page.Height - rect.Y2;
                double width = rect.Width;
                double height = rect.Height;

                sealField.Value = new PdfString("");
                sealField.ReadOnly = true;

                using (var sealStream = new MemoryStream(sealImage))
                {
                    var seal = XImage.FromStream(sealStream);
                    gfx.DrawImage(seal, x, y, width, height);
                }

                // Удаление поля Text9
                RemoveFieldManually(document, "Text9");
            }
        }
        private void RemoveFieldManually(PdfDocument document, string fieldName)
        {
            // Удаление из AcroForm.Fields
            var fieldsArray = document.AcroForm.Elements["/Fields"] as PdfArray;
            if (fieldsArray != null)
            {
                for (int i = fieldsArray.Elements.Count - 1; i >= 0; i--)
                {
                    var fieldRef = fieldsArray.Elements[i];
                    if (fieldRef is PdfReference reference &&
                        reference.Value is PdfDictionary fieldDict &&
                        fieldDict.Elements.ContainsKey("/T") &&
                        fieldDict.Elements["/T"].ToString().Trim('(', ')') == fieldName)
                    {
                        fieldsArray.Elements.RemoveAt(i);
                    }
                }
            }

            // Удаление из аннотаций на всех страницах
            foreach (var page in document.Pages)
            {
                if (page.Elements.ContainsKey("/Annots"))
                {
                    var annots = page.Elements["/Annots"] as PdfArray;
                    if (annots != null)
                    {
                        for (int i = annots.Elements.Count - 1; i >= 0; i--)
                        {
                            var annotRef = annots.Elements[i];
                            if (annotRef is PdfReference reference &&
                                reference.Value is PdfDictionary annotDict &&
                                annotDict.Elements.ContainsKey("/T") &&
                                annotDict.Elements["/T"].ToString().Trim('(', ')') == fieldName)
                            {
                                annots.Elements.RemoveAt(i);
                            }
                        }
                    }
                }
            }
        }


        private void MakeFieldsReadOnly(PdfDocument document)
        {
            if (document.AcroForm == null || document.AcroForm.Fields == null)
                return;

            // Безопасная обработка всех полей формы
            foreach (var field in document.AcroForm.Fields.OfType<PdfTextField>())
            {
                try
                {
                    field.ReadOnly = true;
                }
                catch
                {
                    // Пропускаем поля, которые не могут быть сделаны read-only
                    continue;
                }
            }
        }

        public void SaveCertificateSettings(
            int courseId,
            byte[] templatePdf,
            byte[] sealImage,
            byte[] signatureImage)
        {
            try
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
                INSERT OR REPLACE INTO certificates 
                (course_id, template_pdf, seal_image, signature_image, partner)
                VALUES (@courseId, @templatePdf, @sealImage, @signatureImage, @partner)";

                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@courseId", courseId);
                        cmd.Parameters.AddWithValue("@templatePdf", templatePdf ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@sealImage", sealImage ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@signatureImage", signatureImage ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@partner", DBNull.Value);

                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving certificate settings: {ex.Message}");
                throw;
            }
        }

        public CertificateData LoadCertificateSettings(int courseId)
        {
            try
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
                SELECT 
                    template_pdf, 
                    seal_image, 
                    signature_image,
                    id,
                    partner
                FROM certificates 
                WHERE course_id = @courseId";

                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@courseId", courseId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new CertificateData
                                {
                                    TemplatePdf = reader.IsDBNull(0) ? null : (byte[])reader["template_pdf"],
                                    SealImage = reader.IsDBNull(1) ? null : (byte[])reader["seal_image"],
                                    SignatureImage = reader.IsDBNull(2) ? null : (byte[])reader["signature_image"],
                                    CertificateId = reader.GetInt32(3),
                                    PartnerId = reader.IsDBNull(4) ? 0 : reader.GetInt32(4)
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading certificate settings: {ex.Message}");
            }
            return null;
        }

        public string GenerateCertificateNumber()
        {
            return Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
        }
    }

    public class CertificateData
    {
        public string UserName { get; set; }
        public string CourseTitle { get; set; }
        public string OrganisationName { get; set; }
        public byte[] TemplatePdf { get; set; }
        public byte[] SealImage { get; set; }
        public byte[] SignatureImage { get; set; }
        public int CertificateId { get; set; }
        public DateTime CompletionDate { get; set; }
        public string CertificateNumber { get; set; }
        public int PartnerId { get; set; }
    }
}