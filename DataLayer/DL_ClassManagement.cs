﻿using SchoolGrades.DbClasses;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.IO;

namespace SchoolGrades
{
    internal partial class DataLayer
    {

        internal void DeleteOneStudentFromClass(int? IdDeletingStudent, int? IdClass)
        {
            using (DbConnection conn = Connect())
            {
                DbCommand cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM Classes_Students" +
                    " WHERE Classes_Students.idClass=" + IdClass +
                    " AND Classes_Students.idStudent=" + IdDeletingStudent.ToString() +
                    ";";
                cmd.ExecuteNonQuery();
                cmd.Dispose();
            }
        }

        internal void EraseAllStudentsOfAClass(Class Class)
        {
            using (DbConnection conn = Connect())
            {
                // erase all the info in tables linked to student

                // erase all the grades of the students of the class 
                DbCommand cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM Grades WHERE idStudent IN" +
                    "(SELECT Students.idStudent FROM Students" +
                    " JOIN Classes_Students ON Students.idStudent = Classes_Students.idStudent" +
                    " WHERE Classes_Students.idClass=" + Class.IdClass + ");";
                cmd.ExecuteNonQuery();

                // erase all the questions of the students of the class
                cmd.CommandText = "DELETE FROM StudentsQuestions WHERE idStudent IN" +
                    "(SELECT Students.idStudent FROM Students" +
                    " JOIN Classes_Students ON Students.idStudent = Classes_Students.idStudent" +
                    " WHERE Classes_Students.idClass=" + Class.IdClass + ");";
                cmd.ExecuteNonQuery();

                // erase all the answers of students of the class
                cmd.CommandText = "DELETE FROM StudentsAnswers WHERE idStudent IN" +
                    "(SELECT Students.idStudent FROM Students" +
                    " JOIN Classes_Students ON Students.idStudent = Classes_Students.idStudent" +
                    " WHERE Classes_Students.idClass=" + Class.IdClass + ");";
                cmd.ExecuteNonQuery();

                // erase all the tests of students of the class
                cmd.CommandText = "DELETE FROM StudentsTests WHERE idStudent IN" +
                    "(SELECT Students.idStudent FROM Students" +
                    " JOIN Classes_Students ON Students.idStudent = Classes_Students.idStudent" +
                    " WHERE Classes_Students.idClass=" + Class.IdClass + ");";
                cmd.ExecuteNonQuery();

                // delete all the photos of students of the class 
                cmd.CommandText = "DELETE FROM StudentsPhotos WHERE StudentsPhotos.idStudentsPhoto IN" +
                    "(SELECT StudentsPhotos_Students.idStudentsPhoto" +
                    " FROM StudentsPhotos, StudentsPhotos_Students, Classes_Students" +
                    " WHERE StudentsPhotos_Students.idStudent = Classes_Students.idStudent" +
                    " AND StudentsPhotos.idStudentsPhoto = StudentsPhotos_Students.idStudentsPhoto" +
                    " AND Classes_Students.idClass=" + Class.IdClass + ");";
                cmd.ExecuteNonQuery();

                // delete all the references in link table to photos of students of the class 
                cmd.CommandText = "DELETE FROM StudentsPhotos_Students WHERE idStudent IN" +
                    "(SELECT StudentsPhotos_Students.idStudent" +
                    " FROM StudentsPhotos_Students, Classes_Students" +
                    " WHERE StudentsPhotos_Students.idStudent = Classes_Students.idStudent" +
                    " AND Classes_Students.idClass=" + Class.IdClass + ");";
                cmd.ExecuteNonQuery();

                // delete all the students in class
                // AFTER THIS idStudent OF DELETED IN NOT AVAILABLE ANY LONGER 
                cmd.CommandText = "DELETE FROM Students WHERE idStudent IN" +
                    "(SELECT Students.idStudent FROM Students" +
                    " JOIN Classes_Students ON Students.idStudent = Classes_Students.idStudent" +
                    " WHERE Classes_Students.idClass=" + Class.IdClass + ");";
                cmd.ExecuteNonQuery();

                cmd.Dispose();
            }
        }

        internal void EraseClassFromClasses(Class Class)
        {
            //EraseAllStudentsOfAClass(Class); 
            using (DbConnection conn = Connect())
            {
                // delete all the references in link table between students and classes
                DbCommand cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM Classes_Students" +
                    " WHERE Classes_Students.idClass=" + Class.IdClass +
                    ";";
                cmd.ExecuteNonQuery();
                // erase class from Classes_SchoolSubjects
                cmd.CommandText = "DELETE FROM Classes_SchoolSubjects" +
                    " WHERE Classes_SchoolSubjects.idClass=" + Class.IdClass +
                    ";";
                cmd.ExecuteNonQuery();
                // erase class from Classes_Tests
                cmd.CommandText = "DELETE FROM Classes_Tests" +
                    " WHERE Classes_Tests.idClass=" + Class.IdClass +
                    ";";
                cmd.ExecuteNonQuery();
                // erase class from table Classes 
                cmd.CommandText = "DELETE FROM Classes" +
                    " WHERE Classes.idClass=" + Class.IdClass +
                    ";";
                cmd.ExecuteNonQuery();
                cmd.Dispose();
            }
        }

        internal string CreateOneClassOnlyDatabase(Class Class)
        {
            string newDatabasePathName = Class.PathRestrictedApplication + "\\SchoolGrades\\Data\\";
            if (!Directory.Exists(newDatabasePathName))
                Directory.CreateDirectory(newDatabasePathName);

            string newDatabaseFullName = newDatabasePathName +
                System.DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss") +
                "_" + Class.Abbreviation + "_" + Class.SchoolYear + "_" +
                Commons.FileDatabase;
            File.Copy(Commons.PathAndFileDatabase, newDatabaseFullName);

            // open a local connection to database 
            DataLayer newDatabaseDl = new DataLayer(newDatabaseFullName);

            // erase all the data of the students of other classes
            using (DbConnection conn = newDatabaseDl.Connect())
            {
                DbCommand cmd = conn.CreateCommand();

                // erase all the other classes
                cmd.CommandText = "DELETE FROM Classes" +
                " WHERE idClass<>" + Class.IdClass + ";";
                cmd.ExecuteNonQuery();

                // erase all the lessons of other classes
                cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM Lessons" +
                    " WHERE idClass<>" + Class.IdClass + ";";
                cmd.ExecuteNonQuery();

                // erase all the students of other classes from the link table
                cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM Classes_Students" +
                 " WHERE idClass<>" + Class.IdClass + ";";
                cmd.ExecuteNonQuery();

                // erase all the students of other classes 
                cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM Students" +
                    " WHERE idStudent NOT IN" +
                    " (SELECT idStudent FROM Classes_Students);";
                cmd.ExecuteNonQuery();

                // erase all the StartLinks of other classes
                cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM Classes_StartLinks" +
                    " WHERE idClass<>" + Class.IdClass + ";";
                cmd.ExecuteNonQuery();

                // erase all the grades of other classes' students
                cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM Grades" +
                    " WHERE idStudent NOT IN" +
                    " (SELECT idStudent FROM Classes_Students);";
                cmd.ExecuteNonQuery();

                // erase all the links to photos of other classes' students
                // !! retains previous year's photos of this classes students !!
                cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM StudentsPhotos_Students" +
                    " WHERE idStudent NOT IN" +
                    " (SELECT idStudent FROM Classes_Students);";
                cmd.ExecuteNonQuery();

                // erase all the annotations of other classes
                cmd.CommandText = "DELETE FROM StudentsAnnotations" +
                    " WHERE idStudent NOT IN" +
                    " (SELECT idStudent FROM Classes_Students)" +
                    ";";
                cmd.ExecuteNonQuery();

                // erase all the photos of other classes' students
                // !! retains previous year's photos of this classes students !!
                cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM StudentsPhotos" +
                    " WHERE idStudentsPhoto NOT IN" +
                    " (SELECT idStudentsPhoto FROM StudentsPhotos_Students);";
                cmd.ExecuteNonQuery();

                // erase all the questions of the students of the other classes
                // !! StudentsQuestions currently not used !!
                cmd.CommandText = "DELETE FROM StudentsQuestions" +
                    " WHERE idStudent NOT IN" +
                    " (SELECT idStudent FROM Classes_Students);";
                cmd.ExecuteNonQuery();

                // erase all the answers  of the students of the other classes
                // !! StudentsAnswers currently not used !!
                cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM StudentsAnswers" +
                " WHERE idStudent NOT IN" +
                " (SELECT idStudent FROM Classes_Students);";
                cmd.ExecuteNonQuery();

                // erase all the tests of students of the other classes
                // !! StudentsTests currently not used !!
                cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM StudentsTests" +
                " WHERE idStudent NOT IN" +
                " (SELECT idStudent FROM Classes_Students);";
                cmd.ExecuteNonQuery();

                // erase all the images of other classes' lessons
                cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM Lessons_Images" +
                    " WHERE idLesson NOT IN" +
                    " (SELECT idLesson from Lessons);";
                cmd.ExecuteNonQuery();

                // erase all the topics of other classes' lessons
                cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM Lessons_Topics" +
                    " WHERE idLesson NOT IN" +
                    " (SELECT idLesson from Lessons);";
                cmd.ExecuteNonQuery();

                // erase all the users
                cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM Users" +
                    ";";
                cmd.ExecuteNonQuery();

                // copy all the students' photo files that aren't already there or that have a newer date 
                string query = "SELECT StudentsPhotos.photoPath" +
                " FROM StudentsPhotos" +
                " JOIN StudentsPhotos_Students ON StudentsPhotos_Students.idStudentsPhoto = StudentsPhotos.idStudentsPhoto" +
                " JOIN Classes_Students ON StudentsPhotos_Students.idStudent = Classes_Students.idStudent" +
                " WHERE Classes_Students.idClass = " + Class.IdClass + "; ";
                cmd = new SQLiteCommand(query);
                cmd.Connection = conn;
                DbDataReader dReader = cmd.ExecuteReader();
                while (dReader.Read())
                {
                    string destinationFile = Class.PathRestrictedApplication + "\\SchoolGrades\\Images\\" + (string)dReader["photoPath"];
                    if (!Directory.Exists(Path.GetDirectoryName(destinationFile)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(destinationFile));
                    }
                    if (!File.Exists(destinationFile) ||
                        File.GetLastWriteTime(destinationFile)
                        < File.GetLastWriteTime(Commons.PathImages + "\\" + (string)dReader["photoPath"]))
                        try
                        {
                            // destination file not existing or older
                            File.Copy(Commons.PathImages + "\\" + (string)dReader["photoPath"],
                                destinationFile);
                        }
                        catch { }
                }
                // copy all the picture's files that aren't already there or that have a newer date 
                query = "SELECT Images.imagePath, Classes.pathRestrictedApplication" +
                    " FROM Images" +
                    " JOIN Lessons_Images ON Lessons_Images.idImage=Images.idImage" +
                    " JOIN Lessons ON Lessons_Images.idLesson=Lessons.idLesson" +
                    " JOIN Classes ON Classes.idClass=Lessons.idClass" +
                    " WHERE Lessons.idClass=" + Class.IdClass +
                    ";";
                cmd = new SQLiteCommand(query);
                cmd.Connection = conn;
                dReader = cmd.ExecuteReader();
                while (dReader.Read())
                {
                    if (dReader["pathRestrictedApplication"] is DBNull)
                    {
                        Console.Beep();
                        break;
                    }
                    if (dReader["imagePath"] is DBNull)
                    {
                        Console.Beep();
                        break;
                    }
                    string destinationFile = (string)dReader["pathRestrictedApplication"] +
                        "\\SchoolGrades\\" + "Images" + "\\" + (string)dReader["imagePath"];
                    if (!Directory.Exists(Path.GetDirectoryName(destinationFile)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(destinationFile));
                    }
                    if (!File.Exists(destinationFile) ||
                        File.GetLastWriteTime(destinationFile)
                        < File.GetLastWriteTime(Commons.PathImages + "\\" + (string)dReader["imagePath"]))
                        // destination file not existing or older
                        try
                        {
                            File.Copy(Commons.PathImages + "\\" + (string)dReader["imagePath"],
                                destinationFile);
                        }
                        catch { }
                }
                dReader.Dispose();
                // compact the database 
                cmd.CommandText = "VACUUM;";
                cmd.ExecuteNonQuery();

                cmd.Dispose();
            }
            return Class.PathRestrictedApplication;
        }
        internal int CreateClass(string ClassAbbreviation, string ClassDescription, string SchoolYear,
            string IdSchool)
        {
            // trova una chiave da assegnare alla nuova classe
            int idClass = NextKey("Classes", "idClass");
            using (DbConnection conn = Connect())
            {
                DbCommand cmd = conn.CreateCommand();
                // creazione della classe nella tabella delle classi (soltanto quella) 
                cmd.CommandText = "INSERT INTO Classes " +
                    "(idClass, Desc, idSchoolYear, idSchool, abbreviation) " +
                    "Values (" + idClass + ",'" + SqlVal.SqlString(ClassDescription) + "','" +
                    SqlVal.SqlString(SchoolYear) + "','" + SqlVal.SqlString(IdSchool) + "','" +
                    SqlVal.SqlString(ClassAbbreviation) +
                    "');";
                cmd.ExecuteNonQuery();

                int nextId = NextKey("Classes_StartLinks", "idStartLink");
                cmd = conn.CreateCommand();
                // create a link in StartLinks' link table
                cmd.CommandText = "INSERT INTO Classes_StartLinks " +
                    "(idStartLink,idClass,startLink,desc)" +
                    " Values (" + nextId +
                    "," + idClass + ",'http://www.ingmonti.it','Test link'" +
                    ");";
                cmd.ExecuteNonQuery();
                cmd.Dispose();
            }
            // crea la cartella delle foto della classe, se non esiste
            // if it doesn't exist, create the folder of classes student's images
            if (!Directory.Exists(Commons.PathImages + "\\" + SchoolYear + ClassAbbreviation))
            {
                Directory.CreateDirectory(Commons.PathImages + "\\" + SchoolYear + ClassAbbreviation);
            }
            return idClass;
        }

        internal int CreateClassAndStudents(string[,] StudentsData, string ClassAbbreviation,
                    string ClassDescription, string SchoolYear, string OfficialSchoolAbbreviation,
                    bool LinkPhoto)
        {
            // creation of a new class in the Classes table

            // finds a key for the new class
            int idClass = NextKey("Classes", "idClass");
            using (DbConnection conn = Connect())
            {
                DbCommand cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT INTO Classes " +
                    "(idClass, Desc, idSchoolYear, idSchool, abbreviation) " +
                    "Values (" + idClass + ",'" + SqlVal.SqlString(ClassDescription) + "','" +
                    SqlVal.SqlString(SchoolYear) + "','" + SqlVal.SqlString(OfficialSchoolAbbreviation) + "','" +
                    SqlVal.SqlString(ClassAbbreviation) + "'" +
                    ");";
                cmd.ExecuteNonQuery();

                // find the key for next student
                int idNextStudent = NextKey("Students", "idStudent");
                // find the key for next picture 
                int idNextPhoto = NextKey("StudentsPhotos", "idStudentsPhoto");
                // add the student to the students' table 
                // start from the second row of the file, first row is descriptions 
                for (int riga = 1; riga < StudentsData.GetLength(0); riga++)
                {
                    int rigap1 = riga + 1;
                    // create new student
                    cmd.CommandText = "INSERT INTO Students " +
                        "(idStudent, lastName, firstName, residence, origin, email, birthDate, birthPlace) " +
                        "Values (" +
                        "'" + idNextStudent + "','" +
                        SqlVal.SqlString(StudentsData[riga, 1]) + "','" +
                        SqlVal.SqlString(StudentsData[riga, 2]) + "','" +
                        SqlVal.SqlString(StudentsData[riga, 3]) + "','" +
                        SqlVal.SqlString(StudentsData[riga, 4]) + "','" +
                        SqlVal.SqlString(StudentsData[riga, 5]) + "'," +
                        SqlVal.SqlDate(StudentsData[riga, 6]) + ",'" +
                        SqlVal.SqlString(StudentsData[riga, 7]) + "'" +
                        ");";
                    cmd.ExecuteNonQuery();

                    // aggiunge lo studente alla classe
                    cmd.CommandText = "INSERT INTO Classes_Students " +
                        "(idClass, idStudent, registerNumber) " +
                        "Values ('" + idClass + "','" + idNextStudent + "','" + rigap1.ToString() + "'" +
                        ");";
                    cmd.ExecuteNonQuery();

                    if (LinkPhoto)
                    {
                        // aggiunge la foto alle foto
                        cmd.CommandText = "INSERT INTO StudentsPhotos " +
                            "(idStudentsPhoto, photoPath)" +
                            "Values " +
                            "('" + idNextPhoto + "','" + SqlVal.SqlString(SchoolYear) +
                            SqlVal.SqlString(ClassAbbreviation) + "\\" + SqlVal.SqlString(StudentsData[riga, 1]) + "_" +
                            SqlVal.SqlString(StudentsData[riga, 2]) + "_" + SqlVal.SqlString(ClassAbbreviation) +
                            SqlVal.SqlString(SchoolYear) + ".jpg" + // TODO mettere l'estensione del file che c'è effettivamente
                            "');"; // relative path. Home path will be added at visualization time 
                        cmd.ExecuteNonQuery();

                        // add the picture to the link table
                        cmd.CommandText = "INSERT INTO StudentsPhotos_Students " +
                            "(idStudentsPhoto, idStudent, idSchoolYear) " +
                            "Values (" + idNextPhoto + "," + idNextStudent + ",'" + SqlVal.SqlString(SchoolYear) +
                            "');";
                        cmd.ExecuteNonQuery();
                        idNextPhoto++;
                    }
                    idNextStudent++;
                }
                cmd.Dispose();
            }
            return idClass;
        }

        internal List<Class> GetClassesOfYear(string School, string Year)
        {
            DbDataReader dRead;
            DbCommand cmd;
            List<Class> lc = new List<Class>();

            // Execute the query
            using (DbConnection conn = Connect())
            {
                string query = "SELECT Classes.* " +
                " FROM Classes" +
                " WHERE idSchoolYear = '" + Year + "'" +
                " ORDER BY abbreviation" +
                ";";
                cmd = conn.CreateCommand();
                cmd.CommandText = query;
                dRead = cmd.ExecuteReader();
                // fill the combo with this year's classes
                while (dRead.Read())
                {
                    Class c = new Class((int)dRead["idClass"],
                        (string)dRead["abbreviation"], Year, "dummy");
                    c.UriWebApp = SafeDb.SafeString(dRead["UriWebApp"]);
                    c.PathRestrictedApplication = SafeDb.SafeString(dRead["pathRestrictedApplication"]);

                    lc.Add(c);
                }
                dRead.Dispose();
                cmd.Dispose();
            }
            return lc;
        }

        internal DataTable GetWeightedAveragesOfClass(Class Class,
            string IdGradeType, string IdSchoolSubject, DateTime DateFrom, DateTime DateTo)
        {
            DataTable t;
            using (DbConnection conn = Connect())
            {
                string query = "SELECT Grades.idGrade,Students.idStudent,lastName,firstName" +
                ",SUM(weight)/100 AS 'GradesFraction', 1 - SUM(weight)/100 AS LeftToCloseAssesments" +
                ",COUNT() AS 'GradesCount'" +
                " FROM Classes_Students" +
                " LEFT JOIN Grades ON Students.idStudent=Grades.idStudent" +
                " JOIN Students ON Classes_Students.idStudent=Students.idStudent" +
                " WHERE Classes_Students.idClass =" + Class.IdClass +
                " AND Grades.idSchoolYear='" + Class.SchoolYear + "'" +
                " AND (Grades.idGradeType='" + IdGradeType + "'" +
                " OR Grades.idGradeType IS NULL)" +
                " AND Grades.idSchoolSubject='" + IdSchoolSubject + "'" +
                " AND Grades.value IS NOT NULL AND Grades.value <> 0" +
                " AND Grades.Timestamp BETWEEN " + SqlVal.SqlDate(DateFrom) + " AND " + SqlVal.SqlDate(DateTo) +
                " GROUP BY Students.idStudent" +
                " ORDER BY GradesFraction ASC, lastName, firstName, Students.idStudent;";
                // !!!! TODO change the query to include at first rows also those students that have no grades !!!! 
                DataAdapter DAdapt = new SQLiteDataAdapter(query, (SQLiteConnection)conn);
                DataSet DSet = new DataSet("ClosedMicroGrades");

                DAdapt.Fill(DSet);
                t = DSet.Tables[0];

                DAdapt.Dispose();
                DSet.Dispose();
            }
            return t;
        }

        internal DataTable GetClassTable(int? idClass)
        {
            DataTable t;
            using (DbConnection conn = Connect())
            {
                DataAdapter dAdapter;
                DataSet dSet = new DataSet();

                string query = "SELECT * FROM Classes" +
                " WHERE Classes.idClass = " + idClass + ";";
                dAdapter = new SQLiteDataAdapter(query, (System.Data.SQLite.SQLiteConnection)conn);
                dAdapter.Fill(dSet);
                t = dSet.Tables[0];
                dAdapter.Dispose();
                dSet.Dispose();
            }
            return t;
        }

        internal Class GetClassById(int? IdClass)
        {
            DbDataReader dRead;
            DbCommand cmd;
            Class c = null;
            using (DbConnection conn = Connect())
            {
                string query = "SELECT *" +
                    " FROM Classes" +
                    " WHERE Classes.idClass=" + IdClass +
                    ";";
                cmd = new SQLiteCommand(query);
                dRead = cmd.ExecuteReader();
                while (dRead.Read())
                {
                    c = new Class(IdClass, SafeDb.SafeString(dRead["abbreviation"]), SafeDb.SafeString(dRead["idSchoolYear"]),
                        SafeDb.SafeString(dRead["idSchool"]));
                    c.PathRestrictedApplication = SafeDb.SafeString(dRead["pathRestrictedApplication"]);
                    c.UriWebApp = SafeDb.SafeString(dRead["uriWebApp"]);
                }
                dRead.Dispose();
                cmd.Dispose();
            }
            return c;
        }

        internal DataTable GetClassDataTable(string IdSchool, string IdSchoolYear, string ClassAbbreviation)
        {
            DataTable t;
            using (DbConnection conn = Connect())
            {
                DataAdapter dAdapter;
                DataSet dSet = new DataSet();

                string query = "SELECT DISTINCT registerNumber, Classes.idSchool, Classes.idSchoolYear, " +
                                "Classes.abbreviation, Students.*" +
                " FROM Students, Classes_Students, Classes" +
                " WHERE Students.idStudent = Classes_Students.idStudent AND Classes.idClass = Classes_Students.idClass" +
                    " AND Classes.idSchool = '" + SqlVal.SqlString(IdSchool) + "' AND Classes.idSchoolYear = '" + SqlVal.SqlString(IdSchoolYear) +
                    "' AND Classes.abbreviation = '" + SqlVal.SqlString(ClassAbbreviation) +
                    "' ORDER BY Students.lastName, Students.firstName;";
                dAdapter = new SQLiteDataAdapter(query,
                    (System.Data.SQLite.SQLiteConnection)conn);
                dAdapter.Fill(dSet);
                t = dSet.Tables[0];

                dAdapter.Dispose();
                dSet.Dispose();
            }
            return t;
        }

        internal Class GetClass(string IdSchool, string IdSchoolYear, string ClassAbbreviation)
        {
            Class c = new Class();
            using (DbConnection conn = Connect())
            {
                DbDataReader dRead;
                DbCommand cmd = conn.CreateCommand();
                cmd = conn.CreateCommand();
                string query = "SELECT Classes.*" +
                   " FROM Classes" +
                   " WHERE" +
                   " Classes.idSchoolYear = '" + SqlVal.SqlString(IdSchoolYear) + "'" +
                   " AND Classes.abbreviation = '" + SqlVal.SqlString(ClassAbbreviation) + "'";
                if (IdSchool != null && IdSchool != "")
                    query += " AND Classes.idSchool = '" + SqlVal.SqlString(IdSchool) + "'";
                query += ";";

                cmd.CommandText = query;
                dRead = cmd.ExecuteReader();

                while (dRead.Read())
                {
                    GetClassFromRow(c, dRead);
                    break; // just the first! 
                }
            }
            return c;
        }

        internal Class GetClassOfStudent(string IdSchool, string SchoolYearCode, Student Student)
        {
            Class c = new Class();
            using (DbConnection conn = Connect())
            {
                DbDataReader dRead;
                DbCommand cmd = conn.CreateCommand();
                cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT Classes.*" +
                   " FROM Classes" +
                   " JOIN Classes_Students ON Classes.idClass = Classes_Students.idClass" +
                   " JOIN Students ON Students.idStudent = Classes_Students.idStudent" +
                   " WHERE" +
                   " Classes.idSchool = '" + SqlVal.SqlString(IdSchool) + "'" +
                   " AND Classes.idSchoolYear = '" + SqlVal.SqlString(SchoolYearCode) + "'" +
                   " AND Students.IdStudent = " + Student.IdStudent +
                   ";";
                dRead = cmd.ExecuteReader();

                while (dRead.Read())
                {
                    GetClassFromRow(c, dRead);
                    break; // just the first! 
                }
            }
            return c;
        }

        internal void SaveClass(Class Class)
        {
            //bool leaveConnectionOpen = true;
            //if (conn == null)
            //{
            //    conn = dl.Connect();
            //    leaveConnectionOpen = false;
            //}
            using (DbConnection conn = Connect())
            {
                DbCommand cmd = conn.CreateCommand();
                string query = "UPDATE Classes" +
                    " SET" +
                    " idClass=" + Class.IdClass + "" +
                    ",idSchoolYear='" + SqlVal.SqlString(Class.SchoolYear) + "'" +
                    ",idSchool='" + SqlVal.SqlString(Class.IdSchool) + "'" +
                    ",abbreviation='" + SqlVal.SqlString(Class.Abbreviation) + "'" +
                    ",desc='" + SqlVal.SqlString(Class.Description) + "'" +
                    ",uriWebApp='" + Class.UriWebApp + "'" +
                    ",pathRestrictedApplication='" + SqlVal.SqlString(Class.PathRestrictedApplication) + "'" +
                    " WHERE idClass=" + Class.IdClass +
                    ";";
                cmd.CommandText = query;
                cmd.ExecuteNonQuery();
                cmd.Dispose();
            }
        }

        private void GetClassFromRow(Class Class, DbDataReader Row)
        {
            if (Class == null)
                Class = new Class();
            Class.IdClass = (int)Row["idClass"];
            Class.Abbreviation = SafeDb.SafeString(Row["abbreviation"]);
            Class.IdSchool = SafeDb.SafeString(Row["idSchool"]);
            Class.PathRestrictedApplication = SafeDb.SafeString(Row["pathRestrictedApplication"]);
            Class.SchoolYear = SafeDb.SafeString(Row["idSchoolYear"]);
            Class.UriWebApp = SafeDb.SafeString(Row["uriWebApp"]);
            Class.Description = SafeDb.SafeString(Row["desc"]);
        }
    }
}
