**Objective:**
Create a .NET 8 Console Application that converts quiz data from JSON files into Canvas-compatible IMS QTI 1.2 ZIP packages. The application will use a Windows Forms `OpenFileDialog` to allow the user to select input files.

**File Specifications:**
This project relies on three specification files that define the input and output data structures:
1.  **`quiz-schema.json`**: This file is the **input specification**. It defines the required structure for the JSON files that the user will select. All input files must be validated against this schema.
2.  **`XSD for the IMS ASI Question and Test Interoperability.txt`**: This is an **output specification**. It defines the official XML structure for individual QTI questions and answers (`<item>` elements).
3.  **`DRAFT XSD for IMS Content Packaging version.txt`**: This is an **output specification**. It defines the official XML structure for the `imsmanifest.xml` file, which acts as the table of contents for the entire quiz package.

**Project Setup and Dependencies:**
*   **Framework:** .NET 8
*   **Application Type:** Console Application.
*   **Project File (`.csproj`) Configuration:** The project file must be modified to support the Windows Forms dialog:
    ```xml
    <OutputType>WinExe</OutputType>
    <FrameworkReference Include="Microsoft.WindowsDesktop.App.WindowsForms" />
    ```
*   **Core Libraries:**
    *   **JSON Processing:** Use the built-in `System.Text.Json`.
    *   **ZIP Generation:** Use the built-in `System.IO.Compression`.
    *   **Markdown Conversion:** Use the `Markdig` NuGet package.
    *   **JSON Schema Validation:** Use the `JsonSchema.Net` NuGet package.

**Application Workflow & UI:**
1.  On launch, the application should immediately display a Windows Forms `OpenFileDialog`. The dialog should be configured with a proper title ("Select Quiz JSON Files"), a filter for `.json` files, and `Multiselect` set to `true`.
2.  If the user cancels the dialog, the application should exit gracefully.
3.  If files are selected, the console window will become the primary interface for feedback.
4.  The application will create a folder on the user's Desktop named "QTI_Exports" to store the output ZIP files.
5.  It will then iterate through each selected file path, providing real-time status updates in the console for each one.
6.  After processing all files, a summary report will be displayed in the console.
7.  The console will wait for the user to press any key before closing.

**Core Requirements and Logic:**
1.  **Batch Processing:** The application must process all files selected by the user in the dialog. The processing of each file is an independent operation; a failure in one should not halt the entire process.

2.  **Validation and Error Handling:**
    *   **JSON Schema Validation:** Each JSON file's content must be validated against `quiz-schema.json` using the `JsonSchema.Net` library. If invalid, log a detailed error to the console (e.g., `ERROR in 'history-final.json': Property 'points' at path 'multipleChoiceQuestions[2].points' must be a number.`) and skip to the next file.
    *   **Logical Validation:** The application must verify that each multiple-choice question has **exactly one** answer where `isCorrect` is `true`. If not, log a specific error (e.g., `ERROR in 'history-final.json': Question 'q2' must have exactly one correct answer.`) and skip to the next file.

3.  **Markdown to HTML Conversion:**
    *   The content of the `questionText`, `answers.text`, `modelAnswer`, and `answers.feedback` fields must be treated as Markdown.
    *   Use the `Markdig` library to convert these strings into HTML.
    *   The resulting HTML string must be wrapped in a `<![CDATA[...]]>` section within the final XML to ensure correct rendering by Canvas.

4.  **Answer-Level Feedback:**
    *   The JSON `answers` object array includes an optional `feedback` string. If present, the application must generate a corresponding `<itemfeedback>` element in the QTI XML, linked to that specific answer choice.

5.  **Identifier and File Naming:**
    *   All `identifier` and `ident` attributes in the generated XML must be unique within a single package. Use `Guid.NewGuid()` to generate these identifiers.
    *   The final output `.zip` package for a given JSON file will be named using its `quizId` (e.g., `ims-qti-cp-quiz.zip`) and saved in the "QTI_Exports" folder on the Desktop.
    *   Inside the ZIP, each question's XML file will be named using its `id` from the JSON (e.g., `qti-view-attribute.xml`).

6.  **Console Logging:**
    *   Use the console to provide clear, line-by-line feedback. Use colors for clarity: green for success messages, red for errors, and a neutral color for informational text.
    *   **Example Log for one file:**
        ```
        Processing 'calculus-midterm.json'...
        SUCCESS: Validated against schema.
        SUCCESS: Logical validation passed.
        SUCCESS: Generated 'calculus-midterm-quiz.zip' with 5 questions.
        -------------