﻿using SchoolGrades.DbClasses;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Text;

namespace SchoolGrades
{
    internal partial class DataLayer
    {
        internal Grade GetGrade(int? IdGrade)
        {
            Grade g = null;
            using (DbConnection conn = Connect())
            {
                DbDataReader dRead;
                DbCommand cmd = conn.CreateCommand();
                cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT  * FROM Grades" +
                    " WHERE Grades.idGrade=" + IdGrade.ToString() +
                    ";";
                dRead = cmd.ExecuteReader();
                dRead.Read();
                g = GetGradeFromRow(dRead);
                dRead.Dispose();
                cmd.Dispose();
            }
            return g;
        }

        internal void SaveMacroGrade(int? IdStudent, int? IdParent,
            double Grade, double Weight, string IdSchoolYear,
            string IdSchoolSubject)
        {
            // !!!! TODO !!!! pass a Grade and save all the fields of a grade 
            using (DbConnection conn = Connect())
            {
                DbCommand cmd = conn.CreateCommand();
                // creazione del macrovoto nella tabella dei voti  
                cmd.CommandText = "UPDATE Grades " +
                    "SET IdStudent=" + SqlVal.SqlInt(IdStudent) +
                    ",value=" + SqlVal.SqlDouble(Grade) +
                    ",weight=" + SqlVal.SqlDouble(Weight) +
                    ",idSchoolYear='" + IdSchoolYear + "'" +
                    ",idSchoolSubject='" + IdSchoolSubject +
                    "',timestamp ='" + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss").Replace('.', ':') + "' " +
                    "WHERE idGrade = " + IdParent +
                    ";";
                cmd.ExecuteNonQuery();

                cmd.Dispose();
            }
        }

        internal void GetGradeAndStudent(Grade Grade, Student Student)
        {
            using (DbConnection conn = Connect())
            {
                DbDataReader dRead;
                DbCommand cmd = conn.CreateCommand();
                cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT Grades.*,Students.* FROM Grades" +
                    " JOIN Students ON Grades.idStudent = Students.idStudent" +
                    " WHERE Grades.idGrade=" + Grade.IdGrade.ToString() +
                    ";";
                dRead = cmd.ExecuteReader();
                while (dRead.Read())
                {
                    Grade = GetGradeFromRow(dRead);
                    Student = GetStudentFromRow(dRead);
                    break; // just the first! 
                }
                //dRead.Dispose();
                //cmd.Dispose();
            }
        }

        internal double GetDefaultWeightOfGradeType(string IdGradeType)
        {
            double d;
            using (DbConnection conn = Connect())
            {
                DbCommand cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT defaultWeight " +
                    "FROM GradeTypes " +
                    "WHERE idGradeType='" + IdGradeType + "'; ";
                d = (double)cmd.ExecuteScalar();
                cmd.Dispose();
            }
            return d;
        }

        /// <summary>
        /// Gets all the grades of a students of a specified IdGradeType that are the sons 
        /// of another grade which has value greater than zero
        /// </summary>
        /// <param Name="IdStudent"></param> student's Id
        internal DataTable GetMicroGradesOfStudentWithMacroOpen(int? IdStudent, string IdSchoolYear,
            string IdGradeType, string IdSchoolSubject)
        {
            using (DbConnection conn = Connect())
            {
                // find the macro grade type of the micro grade
                // TODO take it from a Grade passed as parameter 
                DbCommand cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT idGradeTypeParent " +
                    "FROM GradeTypes " +
                    "WHERE idGradeType='" + IdGradeType + "'; ";
                string idGradeTypeParent = (string)cmd.ExecuteScalar();

                string query = "SELECT datetime(Grades.timestamp),Questions.text,Grades.value" +
                    ",Grades.weight,Grades.IdGrade,Grades.idGradeParent,Grades.cncFactor" +
                    ",Questions.IdQuestion,Questions.IdQuestionType,Grades.IdSchoolSubject" +
                    ",Questions.IdTopic,Questions.Image,Questions.Duration,Questions.Difficulty" +
                    ",Questions.*" +
                    ",Grades.*" +
                    " FROM Grades" +
                    " JOIN Grades AS Parents" +
                    " ON Grades.idGradeParent=Parents.idGrade" +
                    " LEFT JOIN Questions" +
                    " ON Grades.idQuestion=Questions.idQuestion" +
                    " WHERE Grades.idStudent =" + IdStudent +
                    " AND Grades.idSchoolYear='" + IdSchoolYear + "'" +
                    " AND Grades.idGradeType = '" + IdGradeType + "'" +
                    " AND Grades.idSchoolSubject = '" + IdSchoolSubject + "'" +
                    " AND Parents.idGradeType = '" + idGradeTypeParent + "'" +
                    " AND Grades.idGradeParent = Parents.idGrade" +
                    " AND (Parents.value = 0 OR Parents.value is NULL)" +
                    " ORDER BY Grades.timestamp;";

                DataAdapter DAdapt = new SQLiteDataAdapter(query, (SQLiteConnection)conn);
                DataSet DSet = new DataSet("OpenMicroGrades");
                DAdapt.Fill(DSet);
                DAdapt.Dispose();
                DSet.Dispose();
                return DSet.Tables[0];
            }
        }

        internal DataTable GetSubGradesOfGrade(int? IdGrade)
        {
            DataTable t;
            using (DbConnection conn = Connect())
            {
                string query = "SELECT datetime(Grades.timestamp),Questions.text,Grades.value," +
                    " Grades.weight,Grades.cncFactor,Grades.idGradeParent" +
                    " FROM Grades" +
                    " JOIN Questions ON Grades.idQuestion=Questions.idQuestion" +
                    " WHERE Grades.idGradeParent =" + IdGrade +
                    " ORDER BY Grades.timestamp;";

                DataAdapter DAdapt = new SQLiteDataAdapter(query, (SQLiteConnection)conn);
                DataSet DSet = new DataSet("OpenMicroGrades");
                DAdapt.Fill(DSet);
                t = DSet.Tables[0];

                DAdapt.Dispose();
                DSet.Dispose();
            }
            return t;
        }

        internal object GetWeightedAveragesOfStudent(Student currentStudent, string stringKey1, string stringKey2, DateTime value1, DateTime value2)
        {
            //throw new NotImplementedException();
            return null;
        }

        internal DataTable GetGradesOfClass(Class Class,
             string IdGradeType, string IdSchoolSubject,
             DateTime DateFrom, DateTime DateTo)
        {
            DataTable t;
            using (DbConnection conn = Connect())
            {
                string query = "SELECT Grades.idGrade,datetime(Grades.timeStamp),Students.idStudent," +
                "lastName,firstName," +
                "Grades.value AS 'grade',Grades.weight," +
                "Grades.idGradeParent" +
                " FROM Grades" +
                " JOIN Students" +
                " ON Students.idStudent=Grades.idStudent" +
                " JOIN Classes_Students" +
                " ON Classes_Students.idStudent=Students.idStudent" +
                " WHERE Classes_Students.idClass =" + Class.IdClass +
                " AND Grades.idSchoolYear='" + Class.SchoolYear + "'" +
                " AND Grades.idGradeType = '" + IdGradeType + "'" +
                " AND Grades.idSchoolSubject = '" + IdSchoolSubject + "'" +
                " AND Grades.Value > 0" +
                " AND Grades.Timestamp BETWEEN " + SqlVal.SqlDate(DateFrom) + " AND " + SqlVal.SqlDate(DateTo) +
                " ORDER BY lastName, firstName, Students.idStudent, Grades.timestamp Desc;";

                DataAdapter DAdapt = new SQLiteDataAdapter(query, (SQLiteConnection)conn);
                DataSet DSet = new DataSet("ClosedMicroGrades");

                DAdapt.Fill(DSet);
                t = DSet.Tables[0];

                DAdapt.Dispose();
                DSet.Dispose();
            }
            return t;
        }

        /// <summary>
        /// Gets the number of microquestions that haven't yet a global grade
        /// </summary>
        /// <param Name="id"></param>
        /// <returns></returns>
        internal List<Grade> CountNonClosedMicroGrades(Class Class, GradeType GradeType)
        {
            DbDataReader dRead;
            DbCommand cmd;
            List<Grade> ls = new List<Grade>();
            using (DbConnection conn = Connect())
            {
                string query = "SELECT Grades.idStudent, Count(*) as nGrades FROM Grades," +
                    "Grades AS Parents,Classes_Students" +
                    " WHERE Classes_Students.idStudent = Grades.idStudent" +
                    " AND Classes_Students.idClass =" + Class.IdClass.ToString() +
                    " AND Grades.idGradeType = '" + GradeType.IdGradeType + "'" +
                    " AND Parents.idGradeType = '" + GradeType.IdGradeTypeParent + "'" +
                    " AND Grades.idGradeParent = Parents.idGrade" +
                    " AND Parents.Value is null or Parents.Value = 0" +
                    " GROUP BY Grades.idStudent;";
                cmd = new SQLiteCommand(query);
                dRead = cmd.ExecuteReader();
                while (dRead.Read())
                {
                    Grade g = new Grade();
                    g.IdStudent = (int)dRead["idStudent"];
                    g.DummyInt = (int)dRead["nGrades"];
                }
                dRead.Dispose();
                cmd.Dispose();
            }
            return ls;
        }

        internal Grade LastGradeOfStudent(Student Student, string IdSchoolYear,
            SchoolSubject SchoolSubject, string IdGradeType)
        {
            DbDataReader dRead;
            DbCommand cmd;
            Grade g = new Grade();
            using (DbConnection conn = Connect())
            {
                cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT Grades.* FROM Grades" +
                    " WHERE Grades.idStudent=" + Student.IdStudent.ToString() +
                    " AND Grades.idSchoolSubject='" + SchoolSubject.IdSchoolSubject + "'" +
                    " AND Grades.idGradeType='" + IdGradeType + "'" +
                    " AND Grades.idSchoolYear='" + IdSchoolYear + "'" +
                    " ORDER BY Grades.timestamp DESC;";
                dRead = cmd.ExecuteReader();
                while (dRead.Read())
                {
                    g = GetGradeFromRow(dRead);
                    break; // just the first! 
                }
                dRead.Dispose();
                cmd.Dispose();
            }
            return g;
        }

        internal void CloneGrade(DataRow Riga)
        {
            using (DbConnection conn = Connect())
            {
                DbCommand cmd = conn.CreateCommand();
                // mette peso 0 nel voto precedente  
                cmd.CommandText = "UPDATE Grades " +
                    " SET weight=0" +
                    " WHERE idGrade = " + Riga["idGrade"] +
                    ";";
                cmd.ExecuteNonQuery();
                // crea un nuovo voto copiato dalla riga passata
                int codiceVoto = NextKey("Grades", "idGrade");

                // aggiunge il voto copiato dalla riga passata
                cmd.CommandText = "INSERT INTO Grades " +
                "(idGrade,idStudent,value,weight,cncFactor,idGradeType,idGradeParent,idSchoolYear" +
                ",timestamp,idQuestion) " +
                "Values (" + codiceVoto + "," + Riga["idStudent"] + "," +
                SqlVal.SqlDouble(Riga["value"]) + "," +
                SqlVal.SqlDouble(Riga["weight"]) + ",'" +
                SqlVal.SqlDouble(Riga["cncFactor"]) + ",'" +
                Riga["idGradeType"] + "'," + Riga["idGradeParent"] + ",'" +
                Riga["idSchoolYear"] + "','" +
                System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss").Replace('.', ':') +
                //"'," + riga["idQuestion"].ToString() + "" +
                "',NULL" +
                ");";
                cmd.ExecuteNonQuery();

                // aggiusta tutte le domande figlie
                cmd.CommandText = "UPDATE Grades " +
                    " SET idGradeParent=" + codiceVoto +
                    " WHERE idGradeParent = " + Riga["idGrade"] +
                    ";";
                cmd.ExecuteNonQuery();

                cmd.Dispose();
            }
        }

        internal List<GradeType> GetListGradeTypes()
        {
            List<GradeType> lg = new List<GradeType>();
            using (DbConnection conn = Connect())
            {
                DbDataReader dRead;
                DbCommand cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT * FROM GradeTypes;";
                dRead = cmd.ExecuteReader();
                while (dRead.Read())
                {
                    GradeType gt = GetGradeTypeFromRow(dRead);
                    lg.Add(gt);
                }
                dRead.Dispose();
                cmd.Dispose();
                return lg;
            }
        }

        internal void DeleteValueOfGrade(int IdGrade)
        {
            using (DbConnection conn = Connect())
            {
                DbCommand cmd = conn.CreateCommand();

                cmd.CommandText = "UPDATE Grades" +
                           " Set" +
                           " value=null" +
                           " WHERE IdGrade=" + IdGrade +
                           ";";
                cmd.ExecuteNonQuery();
                cmd.Dispose();
            }
        }

        internal GradeType GetGradeTypeFromRow(DbDataReader Row)
        {
            if (Row.HasRows)
            {
                GradeType gt = new GradeType();
                gt.IdGradeType = (string)Row["idGradeType"];
                gt.IdGradeTypeParent = SafeDb.SafeString(Row["IdGradeTypeParent"]);
                gt.IdGradeCategory = (string)Row["IdGradeCategory"];
                gt.Name = (string)Row["Name"];
                gt.DefaultWeight = (double)Row["DefaultWeight"];
                gt.Desc = (string)Row["Desc"];
                return gt;
            }
            return null;
        }

        internal GradeType GetGradeType(string IdGradeType)
        {
            GradeType gt = null;
            using (DbConnection conn = Connect())
            {
                DbDataReader dRead;
                DbCommand cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT * FROM GradeTypes";
                cmd.CommandText += " WHERE idGradeType ='" + IdGradeType + "'";
                cmd.CommandText += ";";
                dRead = cmd.ExecuteReader();
                dRead.Read();
                gt = GetGradeTypeFromRow(dRead);
                dRead.Dispose();
                cmd.Dispose();
            }
            return gt;
        }

        internal void RandomizeGrades(DbConnection conn)
        {
            DbDataReader dRead;
            DbCommand cmd = conn.CreateCommand();
            cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM Grades" +
                ";";
            dRead = cmd.ExecuteReader();
            Random rnd = new Random();
            while (dRead.Read())
            {
                double? grade = SafeDb.SafeDouble(dRead["value"]);
                int? id = SafeDb.SafeInt(dRead["IdGrade"]);
                // add to the grade a random delta between -10 and +10 
                if (grade > 0)
                {
                    grade = grade + rnd.NextDouble() * 20 - 10;
                    if (grade < 10) grade = 10;
                    if (grade > 100) grade = 100;
                }
                else
                    grade = 0;
                SaveGradeValue(id, grade, conn);
            }
            cmd.Dispose();
        }

        private void SaveGradeValue(int? id, double? grade, DbConnection conn)
        {
            DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Grades" +
            " SET value=" + SqlVal.SqlDouble(grade) +
            " WHERE idGrade=" + id +
            ";";
            cmd.ExecuteNonQuery();
            cmd.Dispose();
        }
        

        internal void EraseGrade(int? KeyGrade)
        {
            using (DbConnection conn = Connect())
            {
                DbCommand cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM Grades" +
                    " WHERE idGrade=" + KeyGrade +
                    ";";
                cmd.ExecuteNonQuery();
                cmd.Dispose();
            }
        }

        internal Grade GetGradeFromRow(DbDataReader Row)
        {
            Grade g = new Grade();
            g.IdGrade = (int)Row["idGrade"];
            g.IdGradeParent = SafeDb.SafeInt(Row["idGradeParent"]);
            g.IdStudent = SafeDb.SafeInt(Row["idStudent"]);
            g.IdGradeType = SafeDb.SafeString(Row["IdGradeType"]);
            g.IdSchoolSubject = SafeDb.SafeString(Row["IdSchoolSubject"]);
            //g.IdGradeTypeParent = SafeDb.SafeString(Row["idGradeTypeParent"]);
            g.IdQuestion = SafeDb.SafeInt(Row["idQuestion"]);
            g.Timestamp = (DateTime)Row["timestamp"];
            g.Value = SafeDb.SafeDouble(Row["value"]);
            g.Weight = SafeDb.SafeDouble(Row["weight"]);
            g.CncFactor = SafeDb.SafeDouble(Row["cncFactor"]);
            g.IdSchoolYear = SafeDb.SafeString(Row["idSchoolYear"]);
            //g.DummyInt = (int)Row["dummyInt"]; 
            return g;
        }

        internal DataTable GetGradesOfStudent(Student Student, string SchoolYear, string IdGradeType, string IdSchoolSubject,DateTime DateFrom, DateTime DateTo)
        {
            DataTable t;
            using (DbConnection conn = Connect())
            {
                string query = "SELECT Grades.idGrade,datetime(Grades.timeStamp), Students.idStudent," +
                "lastName,firstName," +
                " Grades.value AS 'grade', Grades.weight," +
                " Grades.idGradeParent" +
                " FROM Grades" +
                " JOIN Students" +
                " ON Students.idStudent=Grades.idStudent" +
                " JOIN Classes_Students" +
                " ON Classes_Students.idStudent=Students.idStudent" +
                " WHERE Students.idStudent =" + Student.IdStudent +
                " AND Grades.idSchoolYear='" + SchoolYear + "'" +
                " AND Grades.idGradeType = '" + IdGradeType + "'" +
                " AND Grades.idSchoolSubject = '" + IdSchoolSubject + "'" +
                " AND Grades.Value > 0" +
                " AND Grades.Timestamp BETWEEN " + SqlVal.SqlDate(DateFrom) + " AND " + SqlVal.SqlDate(DateTo) +
                " ORDER BY lastName, firstName, Students.idStudent, Grades.timestamp Desc;";

                DataAdapter DAdapt = new SQLiteDataAdapter(query, (SQLiteConnection)conn);
                DataSet DSet = new DataSet("ClosedMicroGrades");

                DAdapt.Fill(DSet);
                t = DSet.Tables[0];

                DAdapt.Dispose();
                DSet.Dispose();
            }
            return t;
        }
        internal object GetGradesWeightsOfStudentOnOpenGrades(Student currentStudent, string stringKey1, string stringKey2, DateTime value1, DateTime value2)
        {
            throw new NotImplementedException();

        }

        internal DataTable GetGradesWeightedAveragesOfClass(Class Class, string IdGradeType,
            string IdSchoolSubject, DateTime DateFrom, DateTime DateTo)
        {
            DataTable t;
            using (DbConnection conn = Connect())
            {
                string query = "SELECT Grades.idGrade, Students.idStudent,lastName,firstName," +
                " SUM(Grades.value * Grades.weight)/SUM(Grades.weight) AS 'Weighted average'" +
                // weighted RMS (Root Mean Square) as defined here: 
                // https://stackoverflow.com/questions/10947180/weighted-standard-deviation-in-sql-server-without-aggregation-error
                ",SQRT(SUM(Grades.weight * SQUARE(Grades.value)) / SUM(Grades.weight) - SQUARE(SUM(Grades.weight  * Grades.value) / SUM(Grades.weight))) AS 'Weighted RMS'" +
                ",COUNT() 'Grades Count'" +
                " FROM Grades" +
                " JOIN Students" +
                " ON Students.idStudent=Grades.idStudent" +
                " JOIN Classes_Students" +
                " ON Classes_Students.idStudent=Students.idStudent" +
                " WHERE Classes_Students.idClass =" + Class.IdClass +
                " AND Grades.idSchoolYear='" + Class.SchoolYear + "'" +
                " AND Grades.idGradeType = '" + IdGradeType + "'" +
                " AND Grades.idSchoolSubject = '" + IdSchoolSubject + "'" +
                " AND Grades.Value > 0" +
                " AND Grades.Timestamp BETWEEN " + SqlVal.SqlDate(DateFrom) + " AND " + SqlVal.SqlDate(DateTo) +
                " GROUP BY Students.idStudent" +
                " ORDER BY lastName, firstName, Students.idStudent;";

                DataAdapter DAdapt = new SQLiteDataAdapter(query, (SQLiteConnection)conn);
                DataSet DSet = new DataSet("ClosedMicroGrades");

                DAdapt.Fill(DSet);
                t = DSet.Tables[0];

                DAdapt.Dispose();
                DSet.Dispose();
            }
            return t;
        }

        internal DataTable GetUnfixedGrades(Student Student, string IdSchoolSubject,
            double Threshold)
        {
            DataTable t;
            using (DbConnection conn = Connect())
            {
                DataAdapter dAdapt;
                DataSet dSet = new DataSet();
                string query = "SELECT Grades.IdGrade,Grades.idStudent,Grades.value,Grades.timestamp,Grades.isFixed," +
                    "Grades.idGradeType,Grades.idQuestion,Questions.text,Questions.*,Grades.*" +
                    " FROM Grades" +
                    " JOIN Questions ON Grades.idQuestion=Questions.idQuestion" +
                    " WHERE idStudent=" + Student.IdStudent +
                    " AND Grades.value<" + Threshold.ToString() +
                    " AND (Grades.isFixed=0 OR Grades.isFixed is NULL)";
                if (IdSchoolSubject != "")
                    query += " AND Grades.idSchoolSubject='" + IdSchoolSubject + "'";
                query += ";";
                dAdapt = new SQLiteDataAdapter(query, (SQLiteConnection)conn);
                dSet = new DataSet("GetUnfixedGradesInTheYear");
                dAdapt.Fill(dSet);
                t = dSet.Tables[0];

                dSet.Dispose();
                dAdapt.Dispose();
            }
            return t;
        }

        /// <summary>
        /// Gets all the grades of a students of a specified IdGradeType that are the sons 
        /// of another grade which has value NOT null AND NOT equal to zero
        /// </summary>
        /// <param name="IdStudent"></param>
        /// <param name="IdSchoolYear"></param>
        /// <param name="IdGradeType"></param>
        /// <param name="IdSchoolSubject"></param>
        /// <returns></returns>
        internal DataTable GetMacroGradesOfStudentClosed(int? IdStudent, string IdSchoolYear,
            string IdGradeType, string IdSchoolSubject)
        {
            DataTable t;
            using (DbConnection conn = Connect())
            {
                string query = "SELECT idGrade, idStudent, value, idSchoolSubject," +
                    "weight, cncFactor, idSchoolYear, datetime(timestamp), idGradeType, " +
                    "idGradeParent,idQuestion" +
                " FROM Grades" +
                " WHERE Grades.idStudent =" + IdStudent +
                " AND Grades.idSchoolYear='" + IdSchoolYear + "'" +
                " AND Grades.idGradeType = '" + IdGradeType + "'" +
                " AND Grades.idSchoolSubject = '" + IdSchoolSubject + "'" +
                " AND Grades.Value > 0" +
                " ORDER BY datetime(Grades.timestamp) Desc;";

                DataAdapter DAdapt = new SQLiteDataAdapter(query, (SQLiteConnection)conn);
                DataSet DSet = new DataSet("ClosedMicroGrades");

                DAdapt.Fill(DSet);
                t = DSet.Tables[0];

                DAdapt.Dispose();
                DSet.Dispose();
            }
            return t;
        }

        internal int CreateMacroGrade(ref Grade Grade, Student Student, string IdMicroGradeType)
        {
            int key = NextKey("Grades", "idGrade");
            using (DbConnection conn = Connect())
            {
                DbCommand cmd = conn.CreateCommand();
                // find the type of the macro grade of this micrograde
                cmd.CommandText = "SELECT IdGradeTypeParent" +
                    " FROM GradeTypes WHERE idGradetype='" + IdMicroGradeType + "';";
                string IdMacroGrade = (string)cmd.ExecuteScalar();

                // Get the Default Weight of that Grade Type
                cmd.CommandText = "SELECT defaultWeight " +
                    "FROM GradeTypes " +
                    "WHERE idGradeType='" + IdMicroGradeType + "'; ";
                double weight = (double)cmd.ExecuteScalar();

                // add macrograde
                cmd.CommandText = "INSERT INTO Grades " +
                "(idGrade,idStudent,idGradeType,weight,cncFactor,idSchoolYear,timestamp,idSchoolSubject) " +
                "Values (" + key + "," + Student.IdStudent +
                ",'" + IdMacroGrade + "'" +
                "," + SqlVal.SqlDouble(weight) + "" +
                ",0" +
                ",'" + Grade.IdSchoolYear + "','" +
                System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss").Replace('.', ':') + "'" +
                ",'" + Grade.IdSchoolSubject +
                "');";
                cmd.ExecuteNonQuery();

                cmd.Dispose();
            }
            return key;
        }

        internal int? SaveMicroGrade(Grade Grade)
        {
            using (DbConnection conn = Connect())
            {
                DbCommand cmd = conn.CreateCommand();
                // create a new micro assessment in grades table
                if (Grade == null || Grade.IdGrade == null || Grade.IdGrade == 0)
                {
                    Grade.IdGrade = NextKey("Grades", "idGrade");
                    cmd.CommandText = "INSERT INTO Grades " +
                    "(idGrade, idGradeType, idGradeParent, idStudent, value, weight, " +
                    "cncFactor,idSchoolYear, timestamp, idQuestion,idSchoolSubject) " +
                    "Values (" + Grade.IdGrade +
                    ",'" + SqlVal.SqlString(Grade.IdGradeType) + "'" +
                    "," + SqlVal.SqlInt(Grade.IdGradeParent.ToString()) + "" +
                    "," + Grade.IdStudent + "" +
                    "," + SqlVal.SqlDouble(Grade.Value) + "" +
                    "," + SqlVal.SqlDouble(Grade.Weight) + "" +
                    "," + SqlVal.SqlDouble(Grade.CncFactor) + "" +
                    ",'" + SqlVal.SqlString(Grade.IdSchoolYear) + "'" +
                    ",'" + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss").Replace('.', ':') + "'" +
                    "," + SqlVal.SqlInt(Grade.IdQuestion.ToString()) + "" +
                    ",'" + Grade.IdSchoolSubject + "'" +
                    ");";
                }
                else
                {
                    cmd.CommandText = "UPDATE Grades " +
                    "SET" +
                    " idGrade=" + SqlVal.SqlInt(Grade.IdGrade.ToString()) + "" +
                    ",idGradeType='" + SqlVal.SqlString(Grade.IdGradeType) + "'" +
                    ",idGradeParent=" + SqlVal.SqlInt(Grade.IdGradeParent.ToString()) + "" +
                    ",idStudent=" + SqlVal.SqlInt(Grade.IdStudent.ToString()) + "" +
                    ",idSchoolYear='" + SqlVal.SqlString(Grade.IdSchoolYear) + "'" +
                    ",timestamp='" + SqlVal.SqlString(((DateTime)Grade.Timestamp).ToString("yyyy-MM-dd HH:mm:ss")) + "'" +
                    ",idQuestion='" + SqlVal.SqlInt(Grade.IdQuestion.ToString()) + "'" +
                    ",idSchoolSubject='" + SqlVal.SqlString(Grade.IdSchoolSubject) + "'" +
                    ",value=" + SqlVal.SqlDouble(Grade.Value) + "" +
                    ",weight=" + SqlVal.SqlDouble(Grade.Weight) + "" +
                    ",cncFactor=" + SqlVal.SqlDouble(Grade.CncFactor) + "" +
                    " WHERE idGrade=" + SqlVal.SqlInt(Grade.IdGrade.ToString()) +
                    ";";
                }

                cmd.ExecuteNonQuery();

                cmd.Dispose();
            }
            return Grade.IdGrade;
        }

        internal object GetGradesWeightedAveragesOfStudent(Student currentStudent, string stringKey1, string stringKey2, DateTime value1, DateTime value2)
        {
            throw new NotImplementedException();
        }

        internal List<Couple> GetGradesOldestInClass(Class Class,
            GradeType GradeType, SchoolSubject SchoolSubject)
        {
            List<Couple> couples = new List<Couple>();
            using (DbConnection conn = Connect())
            {
                DbDataReader dRead;
                DbCommand cmd = conn.CreateCommand();
                string query;
                query = "SELECT Classes_Students.idStudent" +
                ",MAX(timestamp) AS InstantLastQuestion" +
                " FROM Classes_Students LEFT JOIN Grades" +
                " ON Classes_Students.idStudent = Grades.idStudent" +
                " WHERE Classes_Students.idClass =" + Class.IdClass +
                " AND Grades.idGradeType = '" + GradeType.IdGradeType + "'" +
                " AND Grades.idSchoolSubject='" + SchoolSubject.IdSchoolSubject + "'" +
                " AND Grades.idSchoolYear='" + Class.SchoolYear + "'" +
                " OR Grades.idGrade IS NULL" + // takes those that haven't any grades
                " GROUP BY Classes_Students.idStudent" +
                " ORDER BY InstantLastQuestion DESC;"; // ???? DESC o no 
                cmd.CommandText = query;
                dRead = cmd.ExecuteReader();
                while (dRead.Read())
                {
                    Couple c = new Couple();
                    // we give back also the nulls, as Nows
                    DateTime now = System.DateTime.Now;
                    c.Key = (int)dRead["IdStudent"];
                    if (!dRead.IsDBNull(1))
                        c.Value = SafeDb.SafeDateTime(dRead["InstantLastQuestion"]);
                    else
                        c.Value = now;
                    couples.Add(c);
                }
                dRead.Dispose();
                cmd.Dispose();
            }
            return couples;
        }

        internal DataTable GetGradesWeightsOfClassOnOpenGrades(Class Class,
            string IdGradeType, string IdSchoolSubject, DateTime DateFrom, DateTime DateTo)
        {
            DataTable t;
            using (DbConnection conn = Connect())
            {
                // find the macro grade type of the micro grade
                // TODO take it from a Grade passed as parameter 
                DbCommand cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT idGradeTypeParent " +
                    "FROM GradeTypes " +
                    "WHERE idGradeType='" + IdGradeType + "'; ";
                string idGradeTypeParent = (string)cmd.ExecuteScalar();

                string query = "SELECT Grades.idGrade,Students.idStudent,lastName,firstName" +
                ",SUM(Grades.weight)/100 AS 'GradesFraction', 1 - SUM(Grades.weight)/100 AS LeftToCloseAssesments" +
                ",COUNT() AS 'GradesCount'" +
                " FROM Grades, Grades AS Parents " +
                " JOIN Classes_Students ON Students.idStudent=Grades.idStudent" +
                " JOIN Students ON Classes_Students.idStudent=Students.idStudent" +
                " WHERE Classes_Students.idClass =" + Class.IdClass +
                " AND Grades.idSchoolYear='" + Class.SchoolYear + "'" +
                " AND (Grades.idGradeType='" + IdGradeType + "'" +
                " OR Grades.idGradeType IS NULL)" +
                " AND Grades.idSchoolSubject='" + IdSchoolSubject + "'" +
                " AND Grades.value IS NOT NULL AND Grades.value <> 0" +
                " AND Grades.Timestamp BETWEEN " + SqlVal.SqlDate(DateFrom) + " AND " + SqlVal.SqlDate(DateTo) +
                " AND Parents.idGradeType = '" + idGradeTypeParent + "'" +
                " AND Grades.idGradeParent = Parents.idGrade" +
                " AND (Parents.Value is null or Parents.Value = 0)" +
                " AND NOT Students.disabled" +
                " GROUP BY Students.idStudent" +
                " ORDER BY GradesFraction ASC, lastName, firstName, Students.idStudent;";

                DataAdapter DAdapt = new SQLiteDataAdapter(query, (SQLiteConnection)conn);
                DataSet DSet = new DataSet("ClosedMicroGrades");

                DAdapt.Fill(DSet);
                t = DSet.Tables[0];

                DAdapt.Dispose();
                DSet.Dispose();
            }
            return t;
        }
    }
}
