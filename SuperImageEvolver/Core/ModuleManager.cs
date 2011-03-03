﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Drawing;


namespace SuperImageEvolver {
    public static class ModuleManager {

        static Dictionary<string, IModuleFactory> factoriesByID = new Dictionary<string, IModuleFactory>();
        static Dictionary<string, ModulePreset> presets = new Dictionary<string, ModulePreset>();
        static Dictionary<Type, IModuleFactory> factoriesByType = new Dictionary<Type, IModuleFactory>();

        public static Dictionary<string, ModulePreset> GetPresets( ModuleFunction function ) {
            return presets.Where( p => p.Value.Factory.Function == function ).ToDictionary( k => k.Key, v => v.Value );
        }

        public static void LoadAllPluginAssemblies( string path ) {
            foreach( string file in Directory.GetFiles( path, "*.SIE.dll" ) ) {
                LoadFactories( Assembly.LoadFile( file ) );
            }
        }


        public static void LoadFactories( Assembly assembly ) {
            foreach( Type type in assembly.GetTypes() ) {
                if( type.GetInterfaces().Contains( typeof( IModuleFactory ) ) ) {
                    object newFactory = type.GetConstructor( Type.EmptyTypes ).Invoke( new object[0] );
                    AddModule( newFactory as IModuleFactory );
                }
            }
        }


        public static void AddModule( IModuleFactory factory ) {
            foreach( ModulePreset preset in factory.Presets ) {
                presets.Add( preset.Name, preset );
            }
            factoriesByType.Add( factory.ModuleType, factory );
            factoriesByID.Add( factory.ID, factory );
        }


        public static IModuleFactory GetFactoryByID( string ID ) {
            return factoriesByID[ID];
        }


        public static IModuleFactory GetFactoryByType( Type type ) {
            return factoriesByType[type];
        }


        public static IModule GetPresetByName( string ID ) {
            return presets[ID].GetInstance();
        }


        public static IModule ReadModule( Stream stream ) {
            BinaryReader reader = new BinaryReader( stream );
            string moduleID = reader.ReadString();
            int settingsLength = reader.ReadInt32();
            if( factoriesByID.ContainsKey( moduleID ) ) {
                IModuleFactory factory = GetFactoryByID( moduleID );
                IModule module = factory.GetInstance();
                //module.ReadSettings( reader, settingsLength );
                return module;
            } else {
                stream.Seek( settingsLength, SeekOrigin.Current );
                return null;
            }
        }

        public static IModuleFactory[] ListAllModules() {
            return factoriesByID.Values.ToArray();
        }


        #region Reading Modules

        public static IModule ReadModule( NBTag tag ) {
            string moduleID = tag["ID"].GetString();
            if( !factoriesByID.ContainsKey( moduleID ) ) {
                return null;
            }
            IModuleFactory factory = GetFactoryByID( moduleID );
            IModule module = factory.GetInstance();

            if( tag.Contains( "Properties" ) ) {
            }

            module.ReadSettings( tag["Settings"] );

            return module;
        }

        public static void ReadModuleProperties( IModule module, NBTag tag ) {
            IModuleFactory factory = ModuleManager.GetFactoryByType( module.GetType() );
            foreach( PropertyInfo p in factory.ModuleType.GetProperties() ) {
                if( !tag.Contains( p.Name ) ) continue;
                if( p.PropertyType == typeof( byte ) ) {
                    p.SetValue( module, tag.GetByte(), null );
                } else if( p.PropertyType == typeof( short ) ) {
                    p.SetValue( module, tag.GetShort(), null );
                } else if( p.PropertyType == typeof( int ) ) {
                    p.SetValue( module, tag.GetInt(), null );
                } else if( p.PropertyType == typeof( long ) ) {
                    p.SetValue( module, tag.GetLong(), null );
                } else if( p.PropertyType == typeof( float ) ) {
                    p.SetValue( module, tag.GetFloat(), null );
                } else if( p.PropertyType == typeof( double ) ) {
                    p.SetValue( module, tag.GetDouble(), null );
                } else if( p.PropertyType == typeof( byte[] ) ) {
                    p.SetValue( module, tag.GetBytes(), null );
                } else if( p.PropertyType == typeof( string ) ) {
                    p.SetValue( module, tag.GetString(), null );
                } else if( p.PropertyType == typeof( bool ) ) {
                    p.SetValue( module, tag.GetBool(), null );
                } else if( p.PropertyType == typeof( Color ) ) {
                    p.SetValue( module, tag.GetColor(), null );
                } else if( p.PropertyType == typeof( Point ) ) {
                    p.SetValue( module, tag.GetPoint(), null );
                } else if( p.PropertyType == typeof( PointF ) ) {
                    p.SetValue( module, tag.GetPointF(), null );
                } else {
                    throw new NotSupportedException( "Unknown property type." );
                }
            }
        }

        #endregion


        #region Writing Modules

        public static NBTag WriteModule( IModule module ) {
            NBTCompound root = new NBTCompound();
            IModuleFactory factory = ModuleManager.GetFactoryByType( module.GetType() );
            root.Append( "ID", factory.ID );

            bool auto = !factory.ModuleType.GetCustomAttributes( typeof( DisableAutoSerializationAttribute ), true ).Any();
            if( auto ) {
                root.Append( "Properties", WriteModuleProperties( module ) );
            }
            NBTag customSettings = root.Append( "Settings", new NBTCompound() );
            module.WriteSettings( customSettings );
            return root;
        }


        public static NBTag WriteModuleProperties( IModule module ) {
            IModuleFactory factory = ModuleManager.GetFactoryByType( module.GetType() );
            NBTag root = new NBTCompound();
            foreach( PropertyInfo p in factory.ModuleType.GetProperties() ) {
                object val = p.GetValue( module, null );
                if( p.PropertyType == typeof( byte ) ) {
                    root.Append( p.Name, (byte)val );
                } else if( p.PropertyType == typeof( short ) ) {
                    root.Append( p.Name, (short)val );
                } else if( p.PropertyType == typeof( int ) ) {
                    root.Append( p.Name, (int)val );
                } else if( p.PropertyType == typeof( long ) ) {
                    root.Append( p.Name, (long)val );
                } else if( p.PropertyType == typeof( float ) ) {
                    root.Append( p.Name, (float)val );
                } else if( p.PropertyType == typeof( double ) ) {
                    root.Append( p.Name, (double)val );
                } else if( p.PropertyType == typeof( byte[] ) ) {
                    root.Append( p.Name, (byte[])val );
                } else if( p.PropertyType == typeof( string ) ) {
                    root.Append( p.Name, (string)val );
                } else if( p.PropertyType == typeof( bool ) ) {
                    root.Append( p.Name, (bool)val );
                } else if( p.PropertyType == typeof( Color ) ) {
                    root.Append( p.Name, (Color)val );
                } else if( p.PropertyType == typeof( Point ) ) {
                    root.Append( p.Name, (Point)val );
                } else if( p.PropertyType == typeof( PointF ) ) {
                    root.Append( p.Name, (PointF)val );
                } else {
                    throw new NotSupportedException( "Unknown property type." );
                }
            }
            return root;
        }

        #endregion
    }
}