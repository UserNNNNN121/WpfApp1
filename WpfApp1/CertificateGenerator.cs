
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
        /// <summary>
        /// Генерирует сертификат на основе PDF-шаблона, пользовательских данных и изображений.
        /// Заполняет форму, добавляет изображения, делает поля только для чтения.
        /// </summary>
        /// <param name="userName">Имя пользователя</param>
        /// <param name="courseTitle">Название курса</param>
        /// <param name="organisationName">Название организации</param>
        /// <param name="templatePdf">PDF-шаблон сертификата</param>
        /// <param name="sealImage">Изображение печати</param>
        /// <param name="signatureImage">Изображение подписи</param>
        /// <param name="certificateNumber">Номер сертификата</param>
        /// <param name="completionDate">Дата завершения курса</param>
        /// <returns>Сертификат в виде массива байт</returns>
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
        /// <summary>
        /// Заполняет поля PDF-формы стандартными полями сертификата.
        /// Устанавливает значения и делает поля доступными только для чтения.
        /// </summary>
        /// <param name="document">PDF-документ</param>
        /// <param name="userName">Имя пользователя</param>
        /// <param name="courseTitle">Название курса</param>
        /// <param name="organisationName">Название организации</param>
        /// <param name="certificateNumber">Номер сертификата</param>
        /// <param name="completionDate">Дата завершения</param>
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
        /// <summary>
        /// Устанавливает флаг поля PDF формы "только для чтения".
        /// </summary>
        /// <param name="field">Поле PDF</param>
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

        /// <summary>
        /// Ручное добавление текста в PDF-документ в случае отсутствия формы.
        /// Используется при отсутствии AcroForm в шаблоне.
        /// </summary>
        /// <param name="document">PDF-документ</param>
        /// <param name="userName">Имя пользователя</param>
        /// <param name="courseTitle">Название курса</param>
        /// <param name="organisationName">Название организации</param>
        /// <param name="certificateNumber">Номер сертификата</param>
        /// <param name="completionDate">Дата завершения</param>
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

        /// <summary>
        /// Добавляет изображения подписи и печати в соответствующие поля PDF.
        /// При наличии изображения удаляет исходные поля Text8 и Text9.
        /// </summary>
        /// <param name="document">PDF-документ</param>
        /// <param name="sealImage">Изображение печати</param>
        /// <param name="signatureImage">Изображение подписи</param>
        private void AddImages(PdfDocument document, byte[] sealImage, byte[] signatureImage)
        {
            var page = document.Pages[0];
            var gfx = XGraphics.FromPdfPage(page);

            var fields = document.AcroForm?.Fields;

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

                RemoveFieldManually(document, "Text9");
            }
        }
        /// <summary>
        /// Удаляет указанные поля формы вручную из документа PDF.
        /// Удаление происходит из AcroForm и аннотаций страниц.
        /// </summary>
        /// <param name="document">PDF-документ</param>
        /// <param name="fieldName">Имя поля формы</param>
        private void RemoveFieldManually(PdfDocument document, string fieldName)
        {
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

        /// <summary>
        /// Устанавливает все текстовые поля в PDF-документе как "только для чтения".
        /// Безопасно обрабатывает исключения при установке флагов.
        /// </summary>
        /// <param name="document">PDF-документ</param>
        private void MakeFieldsReadOnly(PdfDocument document)
        {
            if (document.AcroForm == null || document.AcroForm.Fields == null)
                return;

            foreach (var field in document.AcroForm.Fields.OfType<PdfTextField>())
            {
                try
                {
                    field.ReadOnly = true;
                }
                catch
                {
                    continue;
                }
            }
        }
        /// <summary>
        /// Сохраняет шаблон сертификата, изображение печати и подписи в базу данных.
        /// Заменяет или добавляет новую запись для указанного курса.
        /// </summary>
        /// <param name="courseId">ID курса</param>
        /// <param name="templatePdf">Шаблон PDF</param>
        /// <param name="sealImage">Изображение печати</param>
        /// <param name="signatureImage">Изображение подписи</param>
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
        /// <summary>
        /// Загружает данные сертификата из базы данных по идентификатору курса.
        /// </summary>
        /// <param name="courseId">ID курса</param>
        /// <returns>Данные сертификата или null, если не найдено</returns>
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
        /// <summary>
        /// Генерирует уникальный номер сертификата на основе GUID.
        /// </summary>
        /// <returns>Строка с номером сертификата</returns>
        public string GenerateCertificateNumber()
        {
            return Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
        }
    }
    /// <summary>
    /// Класс, содержащий данные сертификата, включая шаблон, изображения и идентификаторы.
    /// Используется для загрузки и генерации сертификатов.
    /// </summary>
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