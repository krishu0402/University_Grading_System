using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Net.Mail;
using System.Net;

namespace UniversityGradingSystem
{
    /// <summary>
    /// Represents a student entity, encapsulating identity and exam history.
    /// Properties and encapsulation support maintainability and extensibility.
    /// </summary>
    public class Student
    {
        /// <summary>
        /// Unique identifier for the student. Used as a primary key for record management.
        /// </summary>
        public string sStudentId { get; set; }

        /// <summary>
        /// Full name of the student. Used for display and search operations.
        /// </summary>
        public string sName { get; set; }

        /// <summary>
        /// Email address of the student. Used for communication and notifications.
        /// </summary>
        public string sEmail { get; set; }

        /// <summary>
        /// List of all exam attempts made by the student. Enables tracking of academic history.
        /// </summary>
        public List<Attempt> Attempts { get; set; }

        /// <summary>
        /// Initializes a new student with the provided ID, name, and optional email.
        /// </summary>
        /// <param name="sStudentId">Unique student ID.</param>
        /// <param name="sname">Student's name.</param>
        /// <param name="semail">Student's email (optional).</param>
        public Student(string sStudentId, string sname, string semail = "")
        {
            this.sStudentId = sStudentId;
            this.sName = sname;
            this.sEmail = semail;
            this.Attempts = new List<Attempt>();
        }

        /// <summary>
        /// Adds a new exam attempt to the student's record.
        /// </summary>
        /// <param name="attempt">The attempt to add.</param>
        public void AddAttempt(Attempt attempt)
        {
            Attempts.Add(attempt);
        }

        /// <summary>
        /// Retrieves the attempt with the highest average score.
        /// Useful for reporting best performance.
        /// </summary>
        /// <returns>Best Attempt or null if none exist.</returns>
        public Attempt GetBestAttempt()
        {
            return Attempts.OrderByDescending(a => a.dAverage).FirstOrDefault();
        }

        /// <summary>
        /// Retrieves the most recent exam attempt.
        /// Useful for generating current marksheets.
        /// </summary>
        /// <returns>Latest Attempt or null if none exist.</returns>
        public Attempt GetLatestAttempt()
        {
            return Attempts.OrderByDescending(a => a.AttemptDate).FirstOrDefault();
        }
    }

    /// <summary>
    /// Models a single exam sitting, encapsulating marks, grade, and status.
    /// </summary>
    public class Attempt
    {
        /// <summary>
        /// Array of marks for each subject in the attempt.
        /// Enables calculation of average and grade.
        /// </summary>
        public int[] iMarks { get; set; }

        /// <summary>
        /// Average score across all subjects for this attempt.
        /// Used to determine grade and pass/fail status.
        /// </summary>
        public double dAverage { get; set; }

        /// <summary>
        /// Pass/fail status based on average score.
        /// </summary>
        public string sStatus { get; set; }

        /// <summary>
        /// Letter grade assigned based on average score.
        /// </summary>
        public string sGrade { get; set; }

        /// <summary>
        /// Date and time when the attempt was made.
        /// Used for sorting and reporting.
        /// </summary>
        public DateTime AttemptDate { get; set; }

        /// <summary>
        /// Sequential number of the attempt for the student.
        /// </summary>
        public int iAttemptNumber { get; set; }

        /// <summary>
        /// Legacy field, not used in current logic.
        /// </summary>
        public int AttemptNumber { get; set; }

        /// <summary>
        /// Legacy field, not used in current logic.
        /// </summary>
        public object Marks { get; private set; }

        /// <summary>
        /// Initializes a new attempt with marks and attempt number.
        /// Automatically calculates results.
        /// </summary>
        /// <param name="marks">Marks for each subject.</param>
        /// <param name="iAttemptNumber">Attempt sequence number.</param>
        public Attempt(int[] marks, int iAttemptNumber)
        {
            this.iMarks = marks;
            this.iAttemptNumber = iAttemptNumber;
            this.AttemptDate = DateTime.Now;
            CalculateResults();
        }

        /// <summary>
        /// Calculates the average, status, and grade for the attempt.
        /// Ensures all derived properties are consistent with marks.
        /// </summary>
        private void CalculateResults()
        {
            if (iMarks != null && iMarks.Length > 0)
            {
                dAverage = iMarks.Average();
                sStatus = dAverage >= 40 ? "Pass" : "Fail";
                sGrade = GetGrade(dAverage);
            }
        }

        /// <summary>
        /// Determines the letter grade based on the average score.
        /// </summary>
        /// <param name="daverage">Average score.</param>
        /// <returns>Letter grade as string.</returns>
        private string GetGrade(double daverage)
        {
            if (daverage >= 70) return "A";
            if (daverage >= 60) return "B";
            if (daverage >= 50) return "C";
            if (daverage >= 40) return "D";
            return "F";
        }
    }

    /// <summary>
    /// Manages all students and their records, and provides the user interface logic.
    /// Handles all core operations such as student creation, marks entry, reporting, and statistics.
    /// </summary>
    public class GradingSystem
    {
        /// <summary>
        /// In-memory list of all students loaded from storage.
        /// Central data structure for all operations.
        /// </summary>
        private List<Student> _students;

        /// <summary>
        /// Directory for storing student record files.
        /// </summary>
        private const string sDATA_DIRECTORY = "StudentRecords";

        /// <summary>
        /// Directory for storing generated marksheet files.
        /// </summary>
        private const string sMARKSHEET_DIRECTORY = "Marksheets";

        /// <summary>
        /// Directory for storing generated academic transcript files.
        /// </summary>
        private const string sTRANSCRIPT_DIRECTORY = "Transcripts";

        /// <summary>
        /// Initializes the grading system, prepares directories, and loads student data.
        /// </summary>
        public GradingSystem()
        {
            _students = new List<Student>();
            InitializeDirectories();
            LoadAllStudents();
        }

        /// <summary>
        /// Ensures all required directories exist for data and report storage.
        /// Handles exceptions to prevent application crash on IO errors.
        /// </summary>
        private void InitializeDirectories()
        {
            try
            {
                if (!Directory.Exists(sDATA_DIRECTORY))
                    Directory.CreateDirectory(sDATA_DIRECTORY);
                if (!Directory.Exists(sMARKSHEET_DIRECTORY))
                    Directory.CreateDirectory(sMARKSHEET_DIRECTORY);
                if (!Directory.Exists(sTRANSCRIPT_DIRECTORY))
                    Directory.CreateDirectory(sTRANSCRIPT_DIRECTORY);
            }
            catch (Exception ex)
            {
                DisplayError($"Error creating directories: {ex.Message}");
            }
        }

        /// <summary>
        /// Main application loop. Displays menu and routes user input to appropriate actions.
        /// Handles exceptions to provide robust user experience.
        /// </summary>
        public void Run()
        {
            DisplayWelcome();
            Console.ReadKey();
            Console.Clear();

            while (true)
            {
                try
                {
                    DisplayMenu();
                    string sChoice = Console.ReadLine();

                    switch (sChoice)
                    {
                        case "1":
                            CreateNewStudent();
                            break;
                        case "2":
                            EnterMarks(false);
                            break;
                        case "3":
                            EnterMarks(true);
                            break;
                        case "4":
                            ViewStudentRecord();
                            break;
                        case "5":
                            SearchBysName();
                            break;
                        case "6":
                            GenerateMarksheet();
                            break;
                        case "7":
                            GenerateAcademicTranscript();
                            break;
                        case "8":
                            ViewAcademicStatistics();
                            break;
                        case "9":
                            SendsEmailToStudent();
                            break;
                        case "10":
                            DisplayStudentTable();
                            break;
                        case "11":
                            ExitApplication();
                            return;
                        default:
                            DisplayError("Invalid selection. Please try again.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    DisplayError($"An error occurred: {ex.Message}");
                }

                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
                Console.Clear();
            }
        }

        /// <summary>
        /// Displays the main menu options to the user.
        /// Uses color for better user experience.
        /// </summary>
        private void DisplayMenu()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n  ╔════════════════════════════════════════╗");
            Console.WriteLine("  ║           MAIN MENU OPTIONS            ║");
            Console.WriteLine("  ╚════════════════════════════════════════╝");
            Console.ResetColor();

            Console.WriteLine("\n1.  Create New Student Record");
            Console.WriteLine("2.  Enter Marks (First Attempt)");
            Console.WriteLine("3.  Enter Marks (Resit Attempt)");
            Console.WriteLine("4.  View Student Record");
            Console.WriteLine("5.  Search Student by Name");
            Console.WriteLine("6.  Generate Marksheet");
            Console.WriteLine("7.  Generate Academic Transcript");
            Console.WriteLine("8.  View Academic Statistics");
            Console.WriteLine("9.  Send Email to Student");
            Console.WriteLine("10. Display Student Table (Dynamic)");
            Console.WriteLine("11. Exit Application");

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("\nPlease select an option (1-11): ");
            Console.ResetColor();
        }

        /// <summary>
        /// Displays a welcome banner and introductory message.
        /// </summary>
        private void DisplayWelcome()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("╔══════════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                      UNIVERSITY OF SUNDERLAND                            ║");
            Console.WriteLine("║                   STUDENT GRADING MANAGEMENT SYSTEM                      ║");
            Console.WriteLine("║                              Est. 1901                                   ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════════════════╝");
            Console.ResetColor();
            Console.WriteLine("\n" + new string('═', 78));
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Welcome to the Advanced Student Assessment Management System");
            Console.WriteLine("Developed for Academic Excellence and Record Management");
            Console.ResetColor();
            Console.WriteLine(new string('═', 78));
            Console.WriteLine("\nPress any key to begin...");
        }

        /// <summary>
        /// Displays a dynamic table of all students and their best performance.
        /// Useful for quick overviews and comparisons.
        /// </summary>
        public void DisplayStudentTable()
        {
            if (!_students.Any())
            {
                Console.WriteLine("No students registered yet.");
                return;
            }

            string[] headers = { "ID", "Name", "Email", "Attempts", "Best Grade", "Best Avg", "Best Status" };
            int idWidth = Math.Max(headers[0].Length, _students.Max(s => s.sStudentId.Length));
            int nameWidth = Math.Max(headers[1].Length, _students.Max(s => s.sName.Length));
            int emailWidth = Math.Max(headers[2].Length, _students.Max(s => (s.sEmail ?? "").Length));
            int attemptsWidth = Math.Max(headers[3].Length, _students.Max(s => s.Attempts.Count.ToString().Length));
            int gradeWidth = headers[4].Length;
            int avgWidth = headers[5].Length;
            int statusWidth = headers[6].Length;

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(
                $"{headers[0].PadRight(idWidth)} | {headers[1].PadRight(nameWidth)} | {headers[2].PadRight(emailWidth)} | {headers[3].PadRight(attemptsWidth)} | {headers[4].PadRight(gradeWidth)} | {headers[5].PadRight(avgWidth)} | {headers[6].PadRight(statusWidth)}"
            );
            Console.ResetColor();
            Console.WriteLine(new string('-', idWidth + nameWidth + emailWidth + attemptsWidth + gradeWidth + avgWidth + statusWidth + 19));

            foreach (var student in _students)
            {
                var best = student.GetBestAttempt();
                string bestGrade = best?.sGrade ?? "N/A";
                string bestAvg = best != null ? best.dAverage.ToString("F2") : "N/A";
                string bestStatus = best?.sStatus ?? "N/A";
                Console.WriteLine(
                    $"{student.sStudentId.PadRight(idWidth)} | {student.sName.PadRight(nameWidth)} | {(student.sEmail ?? "").PadRight(emailWidth)} | {student.Attempts.Count.ToString().PadRight(attemptsWidth)} | {bestGrade.PadRight(gradeWidth)} | {bestAvg.PadRight(avgWidth)} | {bestStatus.PadRight(statusWidth)}"
                );
            }
        }

        /// <summary>
        /// Handles creation of a new student record, including input validation and file persistence.
        /// </summary>
        private void CreateNewStudent()
        {
            DisplaySectionHeader("Create New Student");
            string sStudentId = sGenerateUniquesStudentId();
            Console.WriteLine($"Generated Student ID: {sStudentId}");

            Console.Write("Enter student name: ");
            string sname = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(sname))
            {
                DisplayError("Student name cannot be empty.");
                return;
            }
            Console.Write("Enter student email (optional): ");
            string semail = Console.ReadLine();

            var student = new Student(sStudentId, sname.Trim(), semail.Trim());
            _students.Add(student);
            SaveStudentToFile(student);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\nStudent created successfully!");
            Console.WriteLine($"Student ID: {sStudentId}");
            Console.WriteLine($"Name: {sname}");
            Console.ResetColor();
        }

        /// <summary>
        /// Handles entry of marks for a student, supporting both first and resit attempts.
        /// Validates input and updates student records.
        /// </summary>
        /// <param name="isResit">True if entering a resit attempt, false for first attempt.</param>
        private void EnterMarks(bool isResit)
        {
            string stitle = isResit ? "Enter Resit Marks" : "Enter First Attempt Marks";
            DisplaySectionHeader(stitle);

            Console.Write("Enter Student ID: ");
            string sStudentId = Console.ReadLine();
            var student = FindStudentById(sStudentId);
            if (student == null)
            {
                DisplayError("Student not found.");
                return;
            }

            Console.WriteLine($"Student: {student.sName}");
            if (!isResit && student.Attempts.Any())
            {
                DisplayError("Student already has marks entered. Use resit option for additional attempts.");
                return;
            }

            Console.WriteLine("\nEnter marks for 6 subjects (0-100):");
            var marks = new int[6];
            string[] sSubjects = { "Mathematics", "Physics", "Chemistry", "Biology", "English", "Art" };

            for (int i = 0; i < 6; i++)
            {
                while (true)
                {
                    Console.Write($"{sSubjects[i]}: ");
                    if (int.TryParse(Console.ReadLine(), out int mark) && mark >= 0 && mark <= 100)
                    {
                        marks[i] = mark;
                        break;
                    }
                    DisplayError("Please enter a valid mark between 0 and 100.");
                }
            }
            int iattemptNumber = student.Attempts.Count + 1;
            var attempt = new Attempt(marks, iattemptNumber);
            student.AddAttempt(attempt);
            SaveStudentToFile(student);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\nMarks entered successfully!");
            Console.WriteLine($"Average: {attempt.dAverage:F2}%");
            Console.WriteLine($"Grade: {attempt.sGrade}");
            Console.WriteLine($"Status: {attempt.sStatus}");
            Console.ResetColor();
        }

        /// <summary>
        /// Displays a detailed record of a student, including all attempts and best performance.
        /// </summary>
        private void ViewStudentRecord()
        {
            DisplaySectionHeader("View Student Record");

            Console.Write("Enter Student ID: ");
            string sStudentId = Console.ReadLine();
            var student = FindStudentById(sStudentId);
            if (student == null)
            {
                DisplayError("Student not found.");
                return;
            }
            Console.WriteLine($"\nStudent Details:");
            Console.WriteLine($"ID: {student.sStudentId}");
            Console.WriteLine($"Name: {student.sName}");
            Console.WriteLine($"Email: {student.sEmail}");
            Console.WriteLine($"Total Attempts: {student.Attempts.Count}");

            if (student.Attempts.Any())
            {
                Console.WriteLine("\nAttempt History:");
                foreach (var attempt in student.Attempts)
                {
                    Console.WriteLine($"\nAttempt {attempt.iAttemptNumber} - {attempt.AttemptDate:dd/MM/yyyy}");
                    Console.WriteLine($"Marks: {string.Join(", ", attempt.iMarks)}");
                    Console.WriteLine($"Average: {attempt.dAverage:F2}%");
                    Console.WriteLine($"Grade: {attempt.sGrade}");
                    Console.WriteLine($"Status: {attempt.sStatus}");
                }
                var bestAttempt = student.GetBestAttempt();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\nBest Performance: {bestAttempt.dAverage:F2}% (Grade: {bestAttempt.sGrade})");
                Console.ResetColor();
            }
            else
            {
                Console.WriteLine("\nNo marks entered yet.");
            }
        }

        /// <summary>
        /// Searches for students by name (partial match allowed) and displays results.
        /// </summary>
        private void SearchBysName()
        {
            DisplaySectionHeader("Search Student by Name");

            Console.Write("Enter student name (partial match allowed): ");
            string sSearchsName = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(sSearchsName))
            {
                DisplayError("Search name cannot be empty.");
                return;
            }

            var matchingStudents = _students.Where(s =>
                s.sName != null && s.sName.IndexOf(sSearchsName.Trim(), StringComparison.OrdinalIgnoreCase) >= 0).ToList();

            if (!matchingStudents.Any())
            {
                Console.WriteLine("No students found matching the search criteria.");
                return;
            }

            Console.WriteLine($"\nFound {matchingStudents.Count()} student(s):");
            foreach (var student in matchingStudents)
            {
                Console.WriteLine($"ID: {student.sStudentId}, Name: {student.sName}, Attempts: {student.Attempts.Count}");
            }
        }

        /// <summary>
        /// Generates a marksheet file for the latest attempt of a student.
        /// </summary>
        private void GenerateMarksheet()
        {
            DisplaySectionHeader("Generate Marksheet");

            Console.Write("Enter Student ID: ");
            string sStudentId = Console.ReadLine();

            var student = FindStudentById(sStudentId);
            if (student == null)
            {
                DisplayError("Student not found.");
                return;
            }

            if (!student.Attempts.Any())
            {
                DisplayError("No marks available for this student.");
                return;
            }

            var latestAttempt = student.GetLatestAttempt();
            string sFileName = $"Marksheet_{student.sStudentId}_{DateTime.Now:yyyyMMdd}.txt";
            string sFilePath = Path.Combine(sMARKSHEET_DIRECTORY, sFileName);

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("╔══════════════════════════════════════════════════════════════════════════╗");
                sb.AppendLine("║                      UNIVERSITY OF SUNDERLAND                           ║");
                sb.AppendLine("║                           STUDENT MARKSHEET                             ║");
                sb.AppendLine("╚══════════════════════════════════════════════════════════════════════════╝");
                sb.AppendLine();
                sb.AppendLine($"Student ID: {student.sStudentId}");
                sb.AppendLine($"Name: {student.sName}");
                sb.AppendLine($"Date: {DateTime.Now:dd/MM/yyyy}");
                sb.AppendLine($"Attempt: {latestAttempt.iAttemptNumber}");
                sb.AppendLine();
                sb.AppendLine("Subject Marks:");
                sb.AppendLine("Mathematics: " + latestAttempt.iMarks[0]);
                sb.AppendLine("Physics: " + latestAttempt.iMarks[1]);
                sb.AppendLine("Chemistry: " + latestAttempt.iMarks[2]);
                sb.AppendLine("Biology: " + latestAttempt.iMarks[3]);
                sb.AppendLine("English: " + latestAttempt.iMarks[4]);
                sb.AppendLine("Art:  " + latestAttempt.iMarks[5]);
                sb.AppendLine();
                sb.AppendLine($"Average: {latestAttempt.dAverage:F2}%");
                sb.AppendLine($"Grade: {latestAttempt.sGrade}");
                sb.AppendLine($"Status: {latestAttempt.sStatus}");

                File.WriteAllText(sFilePath, sb.ToString());

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Marksheet generated successfully: {sFileName}");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                DisplayError($"Failed to generate marksheet: {ex.Message}");
            }
        }

        /// <summary>
        /// Generates an academic transcript file for a student, including all attempts and final result.
        /// </summary>
        private void GenerateAcademicTranscript()
        {
            DisplaySectionHeader("Generate Academic Transcript");

            Console.Write("Enter Student ID: ");
            string sStudentId = Console.ReadLine();

            var student = FindStudentById(sStudentId);
            if (student == null)
            {
                DisplayError("Student not found.");
                return;
            }

            if (!student.Attempts.Any())
            {
                DisplayError("No academic records available for this student.");
                return;
            }

            string sFileName = $"Transcript_{student.sStudentId}_{DateTime.Now:yyyyMMdd}.txt";
            string sFilePath = Path.Combine(sTRANSCRIPT_DIRECTORY, sFileName);

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("╔══════════════════════════════════════════════════════════════════════════╗");
                sb.AppendLine("║                      UNIVERSITY OF SUNDERLAND                            ║");
                sb.AppendLine("║                        ACADEMIC TRANSCRIPT                               ║");
                sb.AppendLine("╚══════════════════════════════════════════════════════════════════════════╝");
                sb.AppendLine();
                sb.AppendLine($"Student ID: {student.sStudentId}");
                sb.AppendLine($"Name: {student.sName}");
                sb.AppendLine($"Email: {student.sEmail}");
                sb.AppendLine($"Generated: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
                sb.AppendLine();
                sb.AppendLine("ACADEMIC RECORD:");
                sb.AppendLine(new string('-', 60));

                foreach (var attempt in student.Attempts)
                {
                    sb.AppendLine($"Attempt {attempt.iAttemptNumber} - {attempt.AttemptDate:dd/MM/yyyy}");
                    sb.AppendLine($"Average: {attempt.dAverage:F2}% | Grade: {attempt.sGrade} | Status: {attempt.sStatus}");
                    sb.AppendLine();
                }

                var bestAttempt = student.GetBestAttempt();
                sb.AppendLine("FINAL RESULT:");
                sb.AppendLine($"Best Performance: {bestAttempt.dAverage:F2}%");
                sb.AppendLine($"Final Grade: {bestAttempt.sGrade}");
                sb.AppendLine($"Overall Status: {bestAttempt.sStatus}");

                File.WriteAllText(sFilePath, sb.ToString());

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Academic transcript generated successfully: {sFileName}");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                DisplayError($"Failed to generate transcript: {ex.Message}");
            }
        }

        /// <summary>
        /// Displays overall academic statistics for all students, including pass rates and grade distribution.
        /// </summary>
        private void ViewAcademicStatistics()
        {
            DisplaySectionHeader("Academic Statistics");

            if (!_students.Any())
            {
                Console.WriteLine("No students registered yet.");
                return;
            }

            var studentsWithMarks = _students.Where(s => s.Attempts.Any()).ToList();

            if (!studentsWithMarks.Any())
            {
                Console.WriteLine("No academic records available yet.");
                return;
            }

            var allBestAttempts = studentsWithMarks.Select(s => s.GetBestAttempt()).ToList();
            var passedStudents = allBestAttempts.Where(a => a.sStatus == "Pass").ToList();

            Console.WriteLine($"Total Students: {_students.Count}");
            Console.WriteLine($"Students with Records: {studentsWithMarks.Count}");
            Console.WriteLine($"Passed Students: {passedStudents.Count}");
            Console.WriteLine($"Failed Students: {allBestAttempts.Count - passedStudents.Count}");
            Console.WriteLine($"Pass Rate: {(double)passedStudents.Count / allBestAttempts.Count * 100:F1}%");

            if (allBestAttempts.Any())
            {
                Console.WriteLine($"\nClass Average: {allBestAttempts.Average(a => a.dAverage):F2}%");
                Console.WriteLine($"Highest Score: {allBestAttempts.Max(a => a.dAverage):F2}%");
                Console.WriteLine($"Lowest Score: {allBestAttempts.Min(a => a.dAverage):F2}%");

                var gradeDistribution = allBestAttempts.GroupBy(a => a.sGrade).OrderBy(g => g.Key);
                Console.WriteLine("\nGrade Distribution:");
                foreach (var grade in gradeDistribution)
                {
                    Console.WriteLine($"Grade {grade.Key}: {grade.Count()} students");
                }
            }
        }

        /// <summary>
        /// Simulates sending an email to a student. (SMTP configuration required for real sending.)
        /// </summary>
        private void SendsEmailToStudent()
        {
            DisplaySectionHeader("Send Email to Student");

            Console.Write("Enter Student ID: ");
            string sStudentId = Console.ReadLine();

            var student = FindStudentById(sStudentId);
            if (student == null)
            {
                DisplayError("Student not found.");
                return;
            }

            if (string.IsNullOrWhiteSpace(student.sEmail))
            {
                DisplayError("No email address on file for this student.");
                return;
            }

            Console.Write("Enter email subject: ");
            string sSubject = Console.ReadLine();

            Console.WriteLine("Enter email message (press Enter twice to finish):");
            var message = new StringBuilder();
            string line;
            int emptyLines = 0;

            while (emptyLines < 2)
            {
                line = Console.ReadLine();
                if (string.IsNullOrEmpty(line))
                {
                    emptyLines++;
                }
                else
                {
                    emptyLines = 0;
                    message.AppendLine(line);
                }
            }

            try
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Email functionality requires SMTP configuration.");
                Console.WriteLine($"Email would be sent to: {student.sEmail}");
                Console.WriteLine($"Subject: {sSubject}");
                Console.WriteLine("Message preview:");
                Console.WriteLine(message.ToString());
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                DisplayError($"Failed to send email: {ex.Message}");
            }
        }

        /// <summary>
        /// Displays a colored section header for better UI separation.
        /// </summary>
        /// <param name="sTitle">Section title.</param>
        private void DisplaySectionHeader(string sTitle)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"\n==== {sTitle.ToUpper()} ====\n");
            Console.ResetColor();
        }

        /// <summary>
        /// Displays an error message in red for user feedback.
        /// </summary>
        /// <param name="sMessage">Error message.</param>
        private void DisplayError(string sMessage)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("ERROR: " + sMessage);
            Console.ResetColor();
        }

        /// <summary>
        /// Exits the application with a farewell message.
        /// </summary>
        private void ExitApplication()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\nThank you for using the University Grading System. Goodbye!");
            Console.ResetColor();
        }

        /// <summary>
        /// Generates a unique 8-digit student ID not currently in use.
        /// Ensures no duplicate student records.
        /// </summary>
        /// <returns>Unique student ID as string.</returns>
        private string sGenerateUniquesStudentId()
        {
            var rand = new Random();
            string sId;
            do
            {
                sId = rand.Next(10000000, 99999999).ToString();
            } while (_students.Any(s => s.sStudentId == sId));
            return sId;
        }

        /// <summary>
        /// Finds a student by their unique ID.
        /// Returns null if not found or input is invalid.
        /// </summary>
        /// <param name="sStudentId">Student ID to search for.</param>
        /// <returns>Student object or null.</returns>
        private Student FindStudentById(string sStudentId)
        {
            if (string.IsNullOrWhiteSpace(sStudentId)) return null;
            return _students.FirstOrDefault(s => s.sStudentId == sStudentId.Trim());
        }

        /// <summary>
        /// Saves a student's data and all attempts to a file for persistence.
        /// </summary>
        /// <param name="student">Student to save.</param>
        private void SaveStudentToFile(Student student)
        {
            if (student == null) return;
            string path = Path.Combine(sDATA_DIRECTORY, $"{student.sStudentId}.txt");
            try
            {
                using (var sw = new StreamWriter(path, false, Encoding.UTF8))
                {
                    sw.WriteLine(student.sStudentId);
                    sw.WriteLine(student.sName);
                    sw.WriteLine(student.sEmail ?? "");
                    sw.WriteLine(student.Attempts.Count.ToString());
                    foreach (var attempt in student.Attempts)
                    {
                        sw.WriteLine(string.Join(",", attempt.iMarks));
                        sw.WriteLine(attempt.dAverage.ToString());
                        sw.WriteLine(attempt.sStatus);
                        sw.WriteLine(attempt.sGrade);
                        sw.WriteLine(attempt.AttemptDate.ToString("o"));
                        sw.WriteLine(attempt.iAttemptNumber.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                DisplayError($"Failed to save student file: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads all student records from files in the data directory into memory.
        /// Ignores corrupt or incomplete files for robustness.
        /// </summary>
        private void LoadAllStudents()
        {
            _students.Clear();
            if (!Directory.Exists(sDATA_DIRECTORY)) return;
            foreach (var file in Directory.GetFiles(sDATA_DIRECTORY, "*.txt"))
            {
                try
                {
                    var lines = new List<string>();
                    using (var sr = new StreamReader(file, Encoding.UTF8))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            lines.Add(line);
                        }
                    }
                    if (lines.Count < 4) continue;
                    var student = new Student(lines[0], lines[1], lines[2]);
                    int iattemptCount = int.Parse(lines[3]);
                    int iIdx = 4;
                    for (int i = 0; i < iattemptCount; i++)
                    {
                        if (iIdx + 5 >= lines.Count) break;
                        int[] iMarks = lines[iIdx].Split(',').Select(int.Parse).ToArray();
                        double dAvg = double.Parse(lines[iIdx + 1]);
                        string sStatus = lines[iIdx + 2];
                        string sgrade = lines[iIdx + 3];
                        DateTime date = DateTime.Parse(lines[iIdx + 4]);
                        int iAttemptNum = int.Parse(lines[iIdx + 5]);
                        var attempt = new Attempt(iMarks, iAttemptNum)
                        {
                            dAverage = dAvg,
                            sStatus = sStatus,
                            sGrade = sgrade,
                            AttemptDate = date,
                            iAttemptNumber = iAttemptNum
                        };
                        student.AddAttempt(attempt);
                        iIdx += 6;
                    }
                    _students.Add(student);
                }
                catch
                {
                    // Ignore corrupt files
                }
            }
        }
    }

    /// <summary>
    /// Application entry point. Handles top-level exceptions for robust error reporting.
    /// </summary>
    class Program
    {
        /// <summary>
        /// Main method. Instantiates and runs the grading system.
        /// Catches unhandled exceptions to prevent application crash.
        /// </summary>
        /// <param name="args">Command-line arguments (not used).</param>
        static void Main(string[] args)
        {
            try
            {
                var gradingSystem = new GradingSystem();
                gradingSystem.Run();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Fatal error: {ex.Message}");
                Console.ResetColor();
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }
    }
}
