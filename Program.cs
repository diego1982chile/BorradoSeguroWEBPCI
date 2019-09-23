using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;


namespace cl.trends.pci.WEBPCI.BorradoSeguroWebPCI
{
    class Program
    {        
        static readonly String ERASER_ARGUMENTS = "erase /method=b1bfab4a-31d3-43a5-914c-e9892c78afd8 /target file=";
        static readonly DateTime TODAY = DateTime.Now;
        static String WORKSPACE = @"C:\TEST 3\";
        static String ERASER_PATH = @"C:\Program Files\Eraser\Eraser.exe";
        static int RETENTION = 15;

        static void Main(string[] args)
        {
            try
            {

                readParameters();

                if (!EraserExists())
                {
                    String msg = "No se ha encontrado una instalación de Eraser con la ruta configurada " + ERASER_PATH + ". contacte al Administrador.";
                    Log(msg, EventLogEntryType.Error);
                    throw new System.ApplicationException(msg);
                }

                foreach (String path in GetPathsInWorkSpace())
                {
                    if (IsEligibleForErasure(path))
                    {
                        CallEraserOnFile(path);
                    }
                }
            }
            catch (Exception e)
            {
                Log(e.Message, EventLogEntryType.Error);
                throw e;
            }

            System.Environment.Exit(1);
        }

        static void readParameters()
        {
            try
            {
                FileStream fileStream = new FileStream("parameters.txt", FileMode.Open);

                using (StreamReader reader = new StreamReader(fileStream))
                {
                    while (reader.Peek() >= 0)
                    {
                        string line = reader.ReadLine();

                        string[] tokens = line.Split('=');

                        if (tokens.Length != 2)
                        {
                            String msg = "Formato no válido. Los parámetros deben ser especificados en la forma: [NOMBRE] = [VALOR]";
                            Log(msg, EventLogEntryType.Error);
                            throw new System.ApplicationException(msg);
                        }

                        switch (tokens[0])
                        {
                            case "ERASER_HOME":
                                ERASER_PATH = tokens[1] + @"Eraser.exe";                                
                                break;
                            case "PATHS":
                                WORKSPACE = tokens[1];
                                break;
                            case "RETENTION":
                                RETENTION = Int32.Parse(tokens[1]);
                                break;
                            default:
                                String msg = "Parámetro no válido. Valores aceptados: ERASER_HOME, PATHS, RETENTION";
                                Log(msg, EventLogEntryType.Error);
                                throw new System.ApplicationException(msg);
                        }

                    }                    
                }
            }   
            catch(FileNotFoundException e)
            {
                String msg = "El archivo de parámetros 'parameters.txt' no existe. Debe crear este archivo en la ruta donde se encuentra el ejecutable del aplicativo.";
                Log(msg, EventLogEntryType.Error);
                throw new System.ApplicationException(msg);
            }
            catch(FormatException e2)
            {
                String msg = "Formato no válido. Los parámetros deben ser especificados en la forma: [NOMBRE] = [VALOR]";
                Log(msg, EventLogEntryType.Error);
                throw new System.ApplicationException(msg);
            }            
        }

        static List<String> GetPathsInWorkSpace()
        {
            String drive = Path.GetPathRoot(WORKSPACE);

            if (!Directory.Exists(drive))
            {
                String msg = "Se esperaba disco de red montado en unidad F:\\ Por favor revise que el disco esté " +
                             "conectado en la unidad indicada. Si el problema persiste contacte al Administrador.";
                Log(msg, EventLogEntryType.Error);
                throw new System.ApplicationException(msg);
            }

            if (!Directory.Exists(WORKSPACE))
            {
                String msg = "Se esperaba directorio de descargas. Cree un directorio de descargas correspondiente a " + WORKSPACE +
                             ". Si el problema persiste contacte al Administrador.";
                Log(msg, EventLogEntryType.Error);
                throw new System.ApplicationException(msg);

            }

            return Directory.GetFiles(WORKSPACE).ToList();
        }

        static Boolean IsEligibleForErasure(String path)
        {
            DateTime lastModified = File.GetLastAccessTime(path);

            if ((TODAY - lastModified).TotalDays > RETENTION)
            {
                return true;
            }

            return false;
        }

        static Boolean EraserExists()
        {
            return File.Exists(ERASER_PATH);
        }

        static void CallEraserOnFile(String path)
        {
            try
            {
                Process process = new System.Diagnostics.Process();
                process.StartInfo.FileName = ERASER_PATH;
                process.StartInfo.Arguments = ERASER_ARGUMENTS + '"' + path + '"'; //argument
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                process.StartInfo.CreateNoWindow = true; //not diplay a windows
                process.StartInfo.Verb = "runas";
                process.Start();
                //process.StandardOutput.ReadToEnd();
                //process.WaitForExit();
                while (File.Exists(path))
                {
                    Thread.Sleep(2000);
                }
                process.Kill();
                string output = "Archivo " + path + " borrado exitosamente."; //The output result                
                Log(output, EventLogEntryType.Information);
            }
            catch (Exception e)
            {
                throw new System.ApplicationException(e.Message);
            }

        }

        static void Log(String message, EventLogEntryType level)
        {
            using (EventLog eventLog = new EventLog("Application"))
            {
                eventLog.Source = "cl.trends.pci.WEBPCI.BorradoSeguroWebPCI";
                eventLog.WriteEntry(message, level, 9999, 19 /*Archive Task*/);
            }
        }
    }
}
