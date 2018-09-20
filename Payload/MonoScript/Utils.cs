﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Payload.MonoScript
{
    public static class InjectDebug
    {
        public const string prefix = "__Injector--: ";
        public static void InjectLog(this MonoBehaviour mono, object msg)
        {
            Debug.Log(prefix + mono.GetType().Name + ":\r\n" + msg.ToString());
        }
        public static void InjectLogWarning(this MonoBehaviour mono, object msg)
        {
            Debug.LogWarning(prefix + mono.GetType().Name + ":\r\n" + msg.ToString());
        }
        public static void InjectLogError(this MonoBehaviour mono, object msg, Exception e = null)
        {
            string trace = string.Empty;
            if (e != null)
            {
                trace = "\r\n" + e.Message + "\r\n" + e.StackTrace;
            }
            Debug.LogError(prefix + mono.GetType().Name + ":\r\n" + msg.ToString() + trace);
        }
    }

    public static class AllGUIStyle
    {
        public static readonly GUIStyle DEFAULT_LABEL = new GUIStyle(GUI.skin.label) { fontSize = 13 };
        public static readonly GUIStyle DEFAULT_WINDOW = new GUIStyle(GUI.skin.window) { fontSize = 15 };
    }

    public static class WindowID
    {
        public const int CONSOLE                            = 0x00;
        public const int GAME_STATISTIC                     = 0x01;
        public const int TRANSFORM_WITH_TRIGGER_LIST        = 0x02;
        public const int TRANSFORM_MODIFIER_COMPONENT_LIST  = 0x03;
        public const int TRANSFORM_MODIFIER_PROPERTIES_LIST = 0x04;
        public const int FREE_CAM_VALUES                    = 0x05;
    }

    public static class AllRect
    {
        //Ln1
        public static readonly Rect FreeCamRect      = new Rect(Screen.width * 0.02f, Screen.height * 0.02f, Screen.width * 0.30f, Screen.height * 0.14f);
        public static readonly Rect HierRect         = new Rect(Screen.width * 0.02f, Screen.height * 0.18f, Screen.width * 0.30f, Screen.height * 0.80f);
        //Ln2
        public static readonly Rect PathInputerRect  = new Rect(Screen.width * 0.34f, Screen.height * 0.02f, Screen.width * 0.3f, 20);
        public static readonly Rect CompoRect        = new Rect(Screen.width * 0.34f, Screen.height * 0.02f, Screen.width * 0.3f, Screen.height * 0.40f);
        public static readonly Rect PropRect         = new Rect(Screen.width * 0.34f, Screen.height * 0.44f, Screen.width * 0.3f, Screen.height * 0.54f);
        //Ln3
        public static readonly Rect StatisticRect    = new Rect(Screen.width * 0.66f, Screen.height * 0.02f, Screen.width * 0.32f, Screen.height * 0.46f);
        public static readonly Rect ConsoleRect      = new Rect(Screen.width * 0.66f, Screen.height * 0.50f, Screen.width * 0.32f, Screen.height * 0.48f);
    }
    
    public static class Utils
    {

        public static string Vec32Str(Vector3 v3)
        {
            return " X:" + Rnd(v3.x) + " Y:" + Rnd(v3.y) + " Z:" + Rnd(v3.z);
        }
        public static string Rnd(float f)
        {
            return f.ToString("F2");
        }
        public static float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360)
                angle += 360;
            if (angle > 360)
                angle -= 360;
            return Mathf.Clamp(angle, min, max);
        }
        public static string GetGameObjectPath(GameObject obj)
        {
            string path = "/" + obj.name;
            while (obj.transform.parent != null)
            {
                obj = obj.transform.parent.gameObject;
                path = "/" + obj.name + path;
            }
            return path;
        }
        public static Matrix4x4 ScreenToWorldMatrix(Camera cam)
        {
            var rect = cam.pixelRect;
            var viewportMatrix = Matrix4x4.Ortho(rect.xMin, rect.xMax, rect.yMin, rect.yMax, -1, 1);
            var vpMatrix = cam.projectionMatrix * cam.worldToCameraMatrix;
            vpMatrix.SetColumn(2, new Vector4(0, 0, 1, 0));
            return vpMatrix.inverse * viewportMatrix;
        }
        public static Vector2 ScreenToWorldPointPerspective(Vector2 point,Camera camera)
        {
            return ScreenToWorldMatrix(camera).MultiplyPoint(point);
        }

    }

    public static class Reflector
    {

        public enum VarCata
        {
            Field,
            Property
        }

        public class VariableModifier
        {
            public Component component { get; private set; }
            private List<PropertyInfo> props;
            private List<FieldInfo> fields;
            //List<MethodInfo> methods = new List<MethodInfo>(mono.GetType().GetMethods());

            private List<object> propsModifyCache;
            private List<object> fieldsModifyCache;

            public VariableModifier(Component component)
            {
                this.component = component;
                props = new List<PropertyInfo>(component.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
                fields = new List<FieldInfo>(component.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));

                propsModifyCache = new List<object>();
                for (int i = 0; i < props.Count; i++)
                {
                    propsModifyCache.Add(props[i].CanRead?props[i].GetValue(component, null):"UNREADABLE");
                }

                fieldsModifyCache = new List<object>();
                for (int i = 0; i < fields.Count; i++)
                {
                    fieldsModifyCache.Add(fields[i].GetValue(component));
                }
            }

            public void UpdateCache(VarCata cata, int Pointer)
            {
                if (cata == VarCata.Property)
                        propsModifyCache[Pointer] = props[Pointer].GetValue(component, null);

                if (cata == VarCata.Field)
                    fieldsModifyCache[Pointer] = fields[Pointer].GetValue(component);
            }
            public void SetCache(VarCata cata, int Pointer, object o)
            {
                if (cata == VarCata.Property)
                    propsModifyCache[Pointer] = o;

                if (cata == VarCata.Field)
                    fieldsModifyCache[Pointer] = o;
            }
            public object GetCache(VarCata cata, int Pointer)
            {
                if (cata == VarCata.Property)
                    return propsModifyCache[Pointer];

                if (cata == VarCata.Field)
                    return fieldsModifyCache[Pointer];

                return null;
            }

            public void SetVal(VarCata cata, int Pointer)
            {
                if (cata == VarCata.Property)
                {
                    if (props[Pointer].CanWrite)
                        props[Pointer].SetValue(component, propsModifyCache[Pointer], null);
                    else
                        Debug.LogError(InjectDebug.prefix + "This property is Readonly.");
                }

                if (cata == VarCata.Field)
                    fields[Pointer].SetValue(component, fieldsModifyCache[Pointer]);
            }
            public object GetVal(VarCata cata, int Pointer)
            {
                if (cata == VarCata.Property)
                {
                    if (props[Pointer].CanRead)
                        return props[Pointer].GetValue(component, null);
                    else
                        return "UNREADABLE!";
                }

                if (cata == VarCata.Field)
                    return fields[Pointer].GetValue(component);

                return null;
            }
            public string GetFullName(VarCata cata, int Pointer)
            {
                if (cata == VarCata.Property)
                    return props[Pointer].PropertyType.Name + " " + props[Pointer].Name;

                if (cata == VarCata.Field)
                {
                    string visit = string.Empty;
                    if (fields[Pointer].IsPublic) visit = "Public ";
                    if (fields[Pointer].IsPrivate) visit = "Private ";
                    return visit + (fields[Pointer].IsStatic ? "Static " : "") + fields[Pointer].FieldType.Name + " " + fields[Pointer].Name;
                }

                return null;
            }
            public Type GetType(VarCata cata, int Pointer)
            {
                if (cata == VarCata.Property)
                    return props[Pointer].PropertyType;

                if (cata == VarCata.Field)
                    return fields[Pointer].FieldType;

                return null;
            }
            public bool IsPropertySetable(int Pointer)
            {
                return props[Pointer].CanWrite;
            }

            public int PropertyCount { get { return props.Count; } }
            public int FieldCount { get { return fields.Count; } }

        }

        public static void DrawVarList(VariableModifier vm)
        {
            GUILayout.BeginVertical();

            GUILayout.Label("---------------------------------------------------------------------------");
            for (int i = 0; i < vm.PropertyCount; i++)
            {
                try
                {
                    GUILayout.Label(vm.GetFullName(VarCata.Property, i));
                    GUILayout.Label("Get: " + vm.GetVal(VarCata.Property, i));
                    if(vm.IsPropertySetable(i))VarEditBox(vm, i, VarCata.Property);
                }
                catch (Exception e)
                {
                    Debug.LogError(InjectDebug.prefix + "Something Went Wrong When Drawing Variables!" + "\r\n" + e.Message + "\r\n" + e.StackTrace);
                }
                
                GUILayout.Label("---------------------------------------------------------------------------");
            }
            for (int i = 0; i < vm.FieldCount; i++)
            {
                try
                {
                    GUILayout.Label(vm.GetFullName(VarCata.Field, i) + " = " + vm.GetVal(VarCata.Field, i));
                    VarEditBox(vm, i, VarCata.Field);
                }
                catch (Exception e)
                {
                    Debug.LogError(InjectDebug.prefix + "Something Went Wrong When Drawing Variables!" + "\r\n" + e.Message + "\r\n" + e.StackTrace);
                }

                GUILayout.Label("---------------------------------------------------------------------------");
            }
            


            GUILayout.EndVertical();

        }
        

        private static void VarEditBox(VariableModifier vm,int i, VarCata cata)
        {
            GUILayout.BeginHorizontal();
            
            bool Editable = true;

            Type t = vm.GetType(cata, i);
            object o = vm.GetCache(cata, i);

            if (t == typeof(bool))
                o = GUILayout.Toggle((bool)o, "");
            else if (t == typeof(int))
                o = Convert.ToInt32(GUILayout.TextField(((int)o) + ""));
            else if (t == typeof(float))
                o = Convert.ToSingle(GUILayout.TextField(((float)o) + ""));
            else if (t == typeof(double))
                o = Convert.ToDouble(GUILayout.TextField(((double)o) + ""));
            else if (t == typeof(string))
                o = GUILayout.TextField((string)o);
            else if (t == typeof(Vector2))
            {
                GUILayout.BeginHorizontal();
                o = new Vector2(
                    Convert.ToSingle(GUILayout.TextField(((Vector2)o).x.ToString())),
                    Convert.ToSingle(GUILayout.TextField(((Vector2)o).y.ToString()))
                    );
                GUILayout.EndHorizontal();
            }
            else if (t == typeof(Vector3))
            {
                GUILayout.BeginHorizontal();
                o = new Vector3(
                    Convert.ToSingle(GUILayout.TextField(((Vector3)o).x.ToString())),
                    Convert.ToSingle(GUILayout.TextField(((Vector3)o).y.ToString())),
                    Convert.ToSingle(GUILayout.TextField(((Vector3)o).z.ToString()))
                    );
                GUILayout.EndHorizontal();
            }
            else if (t == typeof(Color))
            {
                GUILayout.BeginHorizontal();
                o = new Color(
                    Convert.ToSingle(GUILayout.HorizontalSlider(((Color)o).r, 0f, 1f)),
                    Convert.ToSingle(GUILayout.HorizontalSlider(((Color)o).g, 0f, 1f)),
                    Convert.ToSingle(GUILayout.HorizontalSlider(((Color)o).b, 0f, 1f)),
                    Convert.ToSingle(GUILayout.HorizontalSlider(((Color)o).a, 0f, 1f))
                    );
                GUILayout.EndHorizontal();
            }
            else Editable = false;

            if (Editable)
            {
                vm.SetCache(cata, i, o);

                if (GUILayout.Button("Update", GUILayout.Width(55)))
                    vm.UpdateCache(cata, i);

                if (GUILayout.Button("Set", GUILayout.Width(35)))
                    vm.SetVal(cata, i);
            }

            GUILayout.EndHorizontal();
        }

    }

}
