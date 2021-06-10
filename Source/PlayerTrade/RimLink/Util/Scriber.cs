using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using Verse;

namespace RimLink.Util
{
    public static class Scriber
    {
        private static readonly FieldInfo Saver_SaveStreamField =
            typeof(ScribeSaver).GetField("saveStream", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo Saver_XmlWriterField =
                    typeof(ScribeSaver).GetField("writer", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo Saver_CurrentPathField =
            typeof(ScribeSaver).GetField("curPath", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo Saver_SavedNodesField =
            typeof(ScribeSaver).GetField("savedNodes", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo Saver_NextTempIdField =
            typeof(ScribeSaver).GetField("nextListElementTemporaryId", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo Loader_CurrentParent =
            typeof(ScribeLoader).GetField("curParent", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo Loader_CurrentXmlParent =
            typeof(ScribeLoader).GetField("curXmlParent", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo Loader_CurrentPathRelToParent =
            typeof(ScribeLoader).GetField("curPathRelToParent", BindingFlags.Instance | BindingFlags.NonPublic);
        
        private static MemoryStream MemoryStream;
        
        public static byte[] Save(string documentElementName, Action save)
        {
            if (Scribe.mode != LoadSaveMode.Inactive)
            {
                Log.Error("Tried to save but current Scribe mode is: " + Scribe.mode);
                Scribe.ForceStop();
            }
            
            if (Saver_CurrentPathField.GetValue(Scribe.saver) != null)
            {
                Log.Error("Current path is not null in InitSaving");
                // Reset everything, similar to what is done in InitSaving, just reimplemented with reflection
                Saver_CurrentPathField.SetValue(Scribe.saver, null);
                ((HashSet<string>) Saver_SavedNodesField.GetValue(Scribe.saver)).Clear();
                Saver_NextTempIdField.SetValue(Scribe.saver, 0);
            }
            
            try
            {
                // Init (effectively reimplementing ScribeSaver.InitSaving)
                Scribe.mode = LoadSaveMode.Saving;
                MemoryStream = new MemoryStream();
                Saver_SaveStreamField.SetValue(Scribe.saver, MemoryStream);
                var xmlWriter = XmlWriter.Create(MemoryStream, new XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = "\t"
                });
                Saver_XmlWriterField.SetValue(Scribe.saver, xmlWriter);
                xmlWriter.WriteStartDocument();
                Scribe.saver.EnterNode(documentElementName);
                
                save();
                Scribe.saver.FinalizeSaving();

                // Get bytes
                byte[] data = MemoryStream.ToArray();
                MemoryStream = null;
                return data;
            }
            catch (Exception e)
            {
                Log.Warn("An exception was thrown during saving: " + e);
                Scribe.saver.ForceStop();
                MemoryStream = null;
                throw;
            }
        }

        public static void Load(byte[] data, Action load)
        {
            if (Scribe.mode != LoadSaveMode.Inactive)
            {
                Log.Error("Tried to load but current Scribe mode is: " + Scribe.mode);
                Scribe.ForceStop();
            }
            if (Scribe.loader.curParent != null)
            {
                Log.Error("Current parent is not null when loading");
                Scribe.loader.curParent = null;
            }
            if (Scribe.loader.curPathRelToParent != null)
            {
                Log.Error("Current path relative to parent is not null when loading");
                Scribe.loader.curPathRelToParent = null;
            }
            
            try
            {
                using (MemoryStream = new MemoryStream(data))
                {
                    using (XmlTextReader xmlTextReader = new XmlTextReader(MemoryStream))
                    {
                        XmlDocument xmlDocument = new XmlDocument();
                        xmlDocument.Load(xmlTextReader);
                        Scribe.loader.curXmlParent = xmlDocument.DocumentElement;
                    }
                }

                Scribe.mode = LoadSaveMode.LoadingVars;
                load();
                Scribe.loader.FinalizeLoading();
            }
            catch (Exception e)
            {
                Log.Error("An exception was thrown during loading: " + e);
            }
        }
        
        public static void Collection<T>(ref List<T> list, string label, LookMode lookMode = LookMode.Undefined)
        {
            Scribe_Collections.Look(ref list, label, lookMode);

            // Not sure why RimWorld doesn't do this natively.
            // Here we initalize a blank list if the list didn't load (wasn't in the save file)
            if (Scribe.mode == LoadSaveMode.PostLoadInit && list == null)
                list = new List<T>();
        }
    }
}