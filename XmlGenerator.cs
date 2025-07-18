using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using CanvasQuizConverter.Models;
using Markdig;

namespace CanvasQuizConverter.Generators
{
    public static class XmlGenerator
    {
        public static string GenerateQuestionItemQti(MultipleChoiceQuestion question)
        {
            var sb = new StringBuilder();
            using (var writer =
                   XmlWriter.Create(sb, new XmlWriterSettings { Indent = true, OmitXmlDeclaration = true }))
            {
                writer.WriteStartElement("questestinterop", "http://www.imsglobal.org/xsd/ims_qtiasiv1p2");
                writer.WriteStartElement("item");
                writer.WriteAttributeString("ident", question.Id);

                writer.WriteStartElement("presentation");
                writer.WriteStartElement("material");
                writer.WriteStartElement("mattext");
                writer.WriteAttributeString("texttype", "text/html");
                writer.WriteCData(Markdown.ToHtml(question.QuestionText ?? ""));
                writer.WriteEndElement(); // mattext
                writer.WriteEndElement(); // material
                writer.WriteStartElement("response_lid");
                writer.WriteAttributeString("ident", "response1");
                writer.WriteAttributeString("rcardinality", "Single");
                writer.WriteStartElement("render_choice");
                foreach (var answer in question.Answers)
                {
                    writer.WriteStartElement("response_label");
                    writer.WriteAttributeString("ident", answer.Id);
                    writer.WriteStartElement("material");
                    writer.WriteStartElement("mattext");
                    writer.WriteAttributeString("texttype", "text/html");
                    writer.WriteCData(Markdown.ToHtml(answer.Text ?? ""));
                    writer.WriteEndElement(); // mattext
                    writer.WriteEndElement(); // material
                    writer.WriteEndElement(); // response_label
                }

                writer.WriteEndElement(); // render_choice
                writer.WriteEndElement(); // response_lid
                writer.WriteEndElement(); // presentation

                writer.WriteStartElement("resprocessing");
                writer.WriteStartElement("outcomes");
                writer.WriteStartElement("decvar");
                writer.WriteAttributeString("maxvalue", "100");
                writer.WriteAttributeString("minvalue", "0");
                writer.WriteAttributeString("varname", "SCORE");
                writer.WriteAttributeString("vartype", "Decimal");
                writer.WriteEndElement(); // decvar
                writer.WriteEndElement(); // outcomes
                var correctAnswer = question.Answers.First(a => a.IsCorrect);
                writer.WriteStartElement("respcondition");
                writer.WriteStartElement("conditionvar");
                writer.WriteStartElement("varequal");
                writer.WriteAttributeString("respident", "response1");
                writer.WriteString(correctAnswer.Id);
                writer.WriteEndElement(); // varequal
                writer.WriteEndElement(); // conditionvar
                writer.WriteStartElement("setvar");
                writer.WriteString("100");
                writer.WriteEndElement(); // setvar
                writer.WriteEndElement(); // respcondition
                writer.WriteEndElement(); // resprocessing

                foreach (var answer in question.Answers.Where(a => !string.IsNullOrEmpty(a.Feedback)))
                {
                    writer.WriteStartElement("itemfeedback");
                    writer.WriteAttributeString("ident", answer.Id);
                    writer.WriteStartElement("flow_mat");
                    writer.WriteStartElement("material");
                    writer.WriteStartElement("mattext");
                    writer.WriteCData(Markdown.ToHtml(answer.Feedback!));
                    writer.WriteEndElement(); // mattext
                    writer.WriteEndElement(); // material
                    writer.WriteEndElement(); // flow_mat
                    writer.WriteEndElement(); // itemfeedback
                }

                writer.WriteEndElement(); // item
                writer.WriteEndElement(); // questestinterop
            }

            return sb.ToString();
        }

        public static string GenerateFreeResponseQti(FreeResponseQuestion question)
        {
            var sb = new StringBuilder();
            using (var writer =
                   XmlWriter.Create(sb, new XmlWriterSettings { Indent = true, OmitXmlDeclaration = true }))
            {
                writer.WriteStartElement("questestinterop", "http://www.imsglobal.org/xsd/ims_qtiasiv1p2");
                writer.WriteStartElement("item");
                writer.WriteAttributeString("ident", question.Id);
                writer.WriteAttributeString("title", "Question");

                writer.WriteStartElement("presentation");
                writer.WriteStartElement("material");
                writer.WriteStartElement("mattext");
                writer.WriteAttributeString("texttype", "text/html");
                writer.WriteCData(Markdown.ToHtml(question.QuestionText ?? ""));
                writer.WriteEndElement(); // mattext
                writer.WriteEndElement(); // material
                writer.WriteStartElement("response_str");
                writer.WriteAttributeString("ident", "response1");
                writer.WriteAttributeString("rcardinality", "String");
                writer.WriteStartElement("render_fib");
                writer.WriteStartElement("response_label");
                writer.WriteAttributeString("ident", "answer1");
                writer.WriteEndElement(); // response_label
                writer.WriteEndElement(); // render_fib
                writer.WriteEndElement(); // response_str
                writer.WriteEndElement(); // presentation

                writer.WriteStartElement("resprocessing");
                writer.WriteStartElement("outcomes");
                writer.WriteStartElement("decvar");
                writer.WriteAttributeString("maxvalue", question.Points.ToString());
                writer.WriteEndElement(); // decvar
                writer.WriteEndElement(); // outcomes
                writer.WriteEndElement(); // resprocessing

                if (!string.IsNullOrEmpty(question.ModelAnswer))
                {
                    writer.WriteStartElement("itemfeedback");
                    writer.WriteAttributeString("ident", "general_feedback");
                    writer.WriteStartElement("flow_mat");
                    writer.WriteStartElement("material");
                    writer.WriteStartElement("mattext");
                    writer.WriteAttributeString("texttype", "text/html");
                    writer.WriteCData(Markdown.ToHtml(question.ModelAnswer));
                    writer.WriteEndElement(); // mattext
                    writer.WriteEndElement(); // material
                    writer.WriteEndElement(); // flow_mat
                    writer.WriteEndElement(); // itemfeedback
                }

                writer.WriteEndElement(); // item
                writer.WriteEndElement(); // questestinterop
            }

            return sb.ToString();
        }

        public static string GenerateAssessmentQti(string quizTitle, string assessmentIdent, List<string> questionIds)
        {
            var sb = new StringBuilder();
            using (var writer =
                   XmlWriter.Create(sb, new XmlWriterSettings { Indent = true, OmitXmlDeclaration = true }))
            {
                writer.WriteStartElement("questestinterop", "http://www.imsglobal.org/xsd/ims_qtiasiv1p2");
                writer.WriteStartElement("assessment");
                writer.WriteAttributeString("ident", assessmentIdent);
                writer.WriteAttributeString("title", quizTitle);
                writer.WriteStartElement("section");
                writer.WriteAttributeString("ident", "root_section");
                foreach (var questionId in questionIds)
                {
                    writer.WriteStartElement("itemref");
                    writer.WriteAttributeString("linkrefid", questionId);
                    writer.WriteEndElement();
                }

                writer.WriteEndElement(); // section
                writer.WriteEndElement(); // assessment
                writer.WriteEndElement(); // questestinterop
            }

            return sb.ToString();
        }

        public static string GenerateImsManifest(string manifestIdent, string assessmentIdent,
            List<string> dependencyIdents, Dictionary<string, string> resources)
        {
            var sb = new StringBuilder();
            using (var writer = XmlWriter.Create(sb, new XmlWriterSettings { Indent = true }))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("manifest", "http://www.imsglobal.org/xsd/imscp_v1p1");
                writer.WriteAttributeString("identifier", manifestIdent);
                writer.WriteAttributeString("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance");
                writer.WriteAttributeString("xsi", "schemaLocation", null,
                    @"http://www.imsglobal.org/xsd/imscp_v1p1 http://www.imsglobal.org/xsd/imscp_v1p1.xsd http://www.imsglobal.org/xsd/ims_qtiasiv1p2 http://www.imsglobal.org/xsd/ims_qtiasiv1p2p1.xsd");

                writer.WriteStartElement("organizations");
                writer.WriteEndElement();

                writer.WriteStartElement("resources");
                writer.WriteStartElement("resource");
                writer.WriteAttributeString("identifier", assessmentIdent);
                writer.WriteAttributeString("type", "imsqti_assessment_xmlv1p2");
                writer.WriteAttributeString("href", $"{assessmentIdent}/{assessmentIdent}.xml");
                writer.WriteStartElement("file");
                writer.WriteAttributeString("href", $"{assessmentIdent}/{assessmentIdent}.xml");
                writer.WriteEndElement(); // file
                foreach (var dependency in dependencyIdents)
                {
                    writer.WriteStartElement("dependency");
                    writer.WriteAttributeString("identifierref", dependency);
                    writer.WriteEndElement(); // dependency
                }

                writer.WriteEndElement(); // resource

                foreach (var resource in resources)
                {
                    writer.WriteStartElement("resource");
                    writer.WriteAttributeString("identifier", resource.Value);
                    writer.WriteAttributeString("type", "imsqti_item_xmlv1p2");
                    writer.WriteAttributeString("href", $"{resource.Value}/{resource.Key}.xml");
                    writer.WriteStartElement("file");
                    writer.WriteAttributeString("href", $"{resource.Value}/{resource.Key}.xml");
                    writer.WriteEndElement(); // file
                    writer.WriteEndElement(); // resource
                }

                writer.WriteEndElement(); // resources
                writer.WriteEndElement(); // manifest
            }

            return sb.ToString();
        }
    }
}