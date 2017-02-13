﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Valve.VR;


namespace SparrowHawk.Interaction
{
    public class Interaction
    {
        protected Scene mScene;

        public void handleInput()
        {
            VREvent_t vrEvent = new VREvent_t();
            unsafe {
                while (mScene.mHMD.PollNextEvent(ref vrEvent, (uint)sizeof(VREvent_t))) {
                    if (mScene.isOculus)
                        oculusInput(ref vrEvent);
                    else
                        viveInput(ref vrEvent);
                }
            }

        }

        protected void oculusInput(ref VREvent_t vrEvent)
        {

        }

        protected void viveInput(ref VREvent_t vrEvent)
        {
            if (vrEvent.eventType == (uint) EVREventType.VREvent_ButtonPress)
            {
                Util.WriteLine(ref mScene.rhinoDoc, "Pressed a button");
                switch (vrEvent.data.controller.button)
                {
                    case (uint) EVRButtonId.k_EButton_SteamVR_Trigger:
                        onClickViveTrigger(ref vrEvent);
                        break;
                    case (uint)EVRButtonId.k_EButton_SteamVR_Touchpad:
                        onClickViveTouchpad(ref vrEvent);
                        break;
                    case (uint)EVRButtonId.k_EButton_Grip:
                        onClickViveGrip(ref vrEvent);
                        break;
                    case (uint)EVRButtonId.k_EButton_ApplicationMenu:
                        onClickViveAppMenu(ref vrEvent);
                        break;
                }
            }
            else if (vrEvent.eventType == (uint)EVREventType.VREvent_ButtonUnpress)
            {
                switch (vrEvent.data.controller.button)
                {
                    case (uint)EVRButtonId.k_EButton_SteamVR_Trigger:
                        onReleaseViveTrigger(ref vrEvent);
                        break;
                    case (uint)EVRButtonId.k_EButton_SteamVR_Touchpad:
                        onReleaseViveTouchpad(ref vrEvent);
                        break;
                    case (uint)EVRButtonId.k_EButton_Grip:
                        onReleaseViveGrip(ref vrEvent);
                        break;
                    case (uint)EVRButtonId.k_EButton_ApplicationMenu:
                        onReleaseViveAppMenu(ref vrEvent);
                        break;
                }
            }
        }

        protected virtual void onClickViveTrigger(ref VREvent_t vrEvent) {}
        protected virtual void onClickViveTouchpad(ref VREvent_t vrEvent) { }
        protected virtual void onClickViveGrip(ref VREvent_t vrEvent) { }
        protected virtual void onClickViveAppMenu(ref VREvent_t vrEvent) { }
        protected virtual void onReleaseViveTrigger(ref VREvent_t vrEvent) { }
        protected virtual void onReleaseViveTouchpad(ref VREvent_t vrEvent) { }
        protected virtual void onReleaseViveGrip(ref VREvent_t vrEvent) { }
        protected virtual void onReleaseViveAppMenu(ref VREvent_t vrEvent) { }
    }
}
