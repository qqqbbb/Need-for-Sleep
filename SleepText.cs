using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static ErrorMessage;

namespace Need_for_Sleep
{
    internal class SleepText : MonoBehaviour
    {
        private static string time = "";
        private static string day = "";
        private static string dayLoc = "";
        private static bool show = false;

        public void Start()
        {
            dayLoc = Language.main.Get("Day");
        }

        public void Update()
        {
            if (!show)
                return;

            DateTime dateTime = DayNightCycle.ToGameDateTime(DayNightCycle.main.timePassedAsFloat);
            int day = (int)Math.Ceiling(DayNightCycle.main.GetDay());
            //AddDebug($"{(int)DayNightCycle.main.GetDay()} day {day}");
            //float dayScalar = DayNightCycle.main.GetDayScalar();
            //int hours = Mathf.FloorToInt(dayScalar * 24f);
            //int minutes = Mathf.FloorToInt(dayScalar % oneHour / oneHour * 60f);
            time = dateTime.Hour.ToString("00") + " : " + dateTime.Minute.ToString("00");
            //time = Language.main.GetFormat("TimeFormatHoursMinutes", dateTime.Hour, dateTime.Minute);
            SleepText.day = dayLoc + ' ' + day;
        }

        public void OnGUI()
        {
            if (!show)
                return;

            float width = Screen.width * .5f;
            float height = Screen.height * .5f;
            GUIStyle dayStyle = new GUIStyle();
            dayStyle.normal.textColor = Color.white;
            dayStyle.fontSize = 64;
            dayStyle.alignment = TextAnchor.LowerCenter;
            GUIStyle timeStyle = new GUIStyle();
            timeStyle.normal.textColor = Color.white;
            timeStyle.fontSize = 64;
            timeStyle.alignment = TextAnchor.UpperCenter;
            float labelWidth = 200f;
            float labelHeight = 40f;
            Rect dayRect = new Rect(width - (labelWidth * 0.5f), height - labelHeight - 22, labelWidth, labelHeight);
            Rect timeRect = new Rect(width - (labelWidth * 0.5f), height, labelWidth, labelHeight);
            GUI.Label(dayRect, day, dayStyle);
            GUI.Label(timeRect, time, timeStyle);
        }

        public static void Show()
        {
            show = true;
        }

        public static void Hide()
        {
            show = false;
        }

    }
}
