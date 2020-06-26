using System;
using VMS.TPS.Common.Model.API;

namespace MeshExport
{
    public class ESAPIApplication
    {

        private static readonly Lazy<ESAPIApplication> _instance = new Lazy<ESAPIApplication>(() => new ESAPIApplication());
        // private to prevent direct instantiation.

        private ESAPIApplication()
        {
            //if user does not have access, this will cause problems
            try
            {
                Context = Application.CreateApplication(null,null);
            }
            catch (Exception ex)
            {
                Environment.Exit(1);
            }
        }

        public Application Context { get; private set; }
        public static bool IsLoaded { get; set; }

        // accessor for instance
        public static ESAPIApplication Instance {
            get {
                IsLoaded = true;
                return _instance.Value;
            }
        }

        public static void Dispose()
        {
            if (IsLoaded) { Instance.Context.Dispose(); }
        }
    }

    }

