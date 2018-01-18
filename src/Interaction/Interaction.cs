using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Valve.VR;


namespace SparrowHawk.Interaction
{
    public class Interaction
    {
        protected Scene mScene;
        protected int primaryControllerIdx;
        protected int secondaryControllerIdx;
        protected bool inChain = false;

        protected Interaction(ref Scene scene)
        {
            mScene = scene;
            if (mScene.mIsLefty)
            {
                primaryControllerIdx = mScene.leftControllerIdx;
                secondaryControllerIdx = mScene.rightControllerIdx;
            }
            else
            {
                primaryControllerIdx = mScene.rightControllerIdx;
                secondaryControllerIdx = mScene.leftControllerIdx;
            }
        }

        public void setInChain(bool inChain)
        {
            this.inChain = inChain;
        }

        public bool isInChain()
        {
            return inChain;
        }

        public void handleInput()
        {
            VREvent_t vrEvent = new VREvent_t();
            unsafe
            {
                while (mScene.mHMD.PollNextEvent(ref vrEvent, (uint)sizeof(VREvent_t)))
                {
                    if (mScene.isOculus)
                        oculusInput(ref vrEvent);
                    else
                        viveInput(ref vrEvent);
                }
            }

        }

        protected void oculusInput(ref VREvent_t vrEvent)
        {
            if (vrEvent.eventType == (uint)EVREventType.VREvent_ButtonPress)
            {
                //Rhino.RhinoApp.WriteLine("Pressed an oculus button");
                switch (vrEvent.data.controller.button)
                {
                    case (uint)UtilOld.OculusButtonId.k_EButton_Oculus_AX:
                        onClickOculusAX(ref vrEvent);
                        break;
                    case (uint)UtilOld.OculusButtonId.k_EButton_Oculus_BY:
                        onClickOculusBY(ref vrEvent);
                        break;
                    case (uint)UtilOld.OculusButtonId.k_EButton_Oculus_Grip:
                        onClickOculusGrip(ref vrEvent);
                        break;
                    case (uint)UtilOld.OculusButtonId.k_EButton_Oculus_Stick:
                        onClickOculusStick(ref vrEvent);
                        break;
                    case (uint)UtilOld.OculusButtonId.k_EButton_Oculus_Trigger:
                        onClickOculusTrigger(ref vrEvent);
                        break;
                }
            }
            else if (vrEvent.eventType == (uint)EVREventType.VREvent_ButtonUnpress)
            {
                switch (vrEvent.data.controller.button)
                {
                    case (uint)UtilOld.OculusButtonId.k_EButton_Oculus_AX:
                        onReleaseOculusAX(ref vrEvent);
                        break;
                    case (uint)UtilOld.OculusButtonId.k_EButton_Oculus_BY:
                        onReleaseOculusBY(ref vrEvent);
                        break;
                    case (uint)UtilOld.OculusButtonId.k_EButton_Oculus_Grip:
                        onReleaseOculusGrip(ref vrEvent);
                        break;
                    case (uint)UtilOld.OculusButtonId.k_EButton_Oculus_Stick:
                        onReleaseOculusStick(ref vrEvent);
                        break;
                    case (uint)UtilOld.OculusButtonId.k_EButton_Oculus_Trigger:
                        onReleaseOculusTrigger(ref vrEvent);
                        break;
                }
            }

            else if (vrEvent.eventType == (uint)EVREventType.VREvent_ButtonUntouch)
            {
                switch (vrEvent.data.controller.button)
                {
                    case (uint)UtilOld.OculusButtonId.k_EButton_Oculus_Stick:
                        onUntouchOculusStick(ref vrEvent);
                        break;
                }
            }
        }

        protected void viveInput(ref VREvent_t vrEvent)
        {
            if (vrEvent.eventType == (uint)EVREventType.VREvent_ButtonPress)
            {
                //Rhino.RhinoApp.WriteLine("Pressed a button");
                switch (vrEvent.data.controller.button)
                {
                    case (uint)EVRButtonId.k_EButton_SteamVR_Trigger:
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

            else if (vrEvent.eventType == (uint)EVREventType.VREvent_ButtonUntouch)
            {
                switch (vrEvent.data.controller.button)
                {
                    case (uint)EVRButtonId.k_EButton_SteamVR_Touchpad:
                        onUntouchViveTouchpad(ref vrEvent);
                        break;
                }
            }
        }
        /// <summary>
        /// Gets called when this interaction is get to the top of the stack. (last interaction pop)
        /// </summary>
        public virtual void init() { }

        /// <summary>
        /// Called every frame while this interaction is in the interaction stack.
        /// If it is at the top of the stack, isTop will evaluate to true. 
        /// Otherwise, isTop will evaluate to false. This allows us to 
        /// </summary>
        /// <param name="isTop"> True iff this is the top interaction in the stack.</param>
        public virtual void draw(bool isTop) { }

        /// <summary>
        /// Gets called whenever this interaction is placed on the 
        /// top of the stack.
        /// </summary>
        public virtual void activate() { }

        /// <summary>
        /// Gets called when this interaction is removed from the stack.
        /// </summary>
        public virtual void deactivate()
        {

        }

        /// <summary>
        /// Gets called when this interaction leave top of the stack).
        /// </summary>
        public virtual void leaveTop()
        {

        }

        protected virtual void onClickViveTrigger(ref VREvent_t vrEvent) { }
        protected virtual void onClickViveTouchpad(ref VREvent_t vrEvent) { }
        protected virtual void onClickViveGrip(ref VREvent_t vrEvent) { }
        protected virtual void onClickViveAppMenu(ref VREvent_t vrEvent) { }
        protected virtual void onReleaseViveTrigger(ref VREvent_t vrEvent) { }
        protected virtual void onReleaseViveTouchpad(ref VREvent_t vrEvent) { }
        protected virtual void onReleaseViveGrip(ref VREvent_t vrEvent) { }
        protected virtual void onReleaseViveAppMenu(ref VREvent_t vrEvent) { }
        protected virtual void onUntouchViveTouchpad(ref VREvent_t vrEvent) { }
        protected virtual void onClickOculusTrigger(ref VREvent_t vrEvent) { }
        protected virtual void onClickOculusStick(ref VREvent_t vrEvent)
        {

        }
        protected virtual void onClickOculusGrip(ref VREvent_t vrEvent) { }
        protected virtual void onClickOculusAX(ref VREvent_t vrEvent)
        {
            if (vrEvent.trackedDeviceIndex == mScene.leftControllerIdx)
            {
                mScene.pushInteraction(new Cut(ref mScene));
            }
        }
        protected virtual void onClickOculusBY(ref VREvent_t vrEvent)
        {

            //back button - make sure each interaction implements deactivate() properly
            Interaction i = mScene.popInteraction();

            //push it back to interactionchain
            if (i.isInChain())
            {
                mScene.pushInteractionToChain(i);
            }

        }
        protected virtual void onReleaseOculusTrigger(ref VREvent_t vrEvent) { }
        protected virtual void onReleaseOculusStick(ref VREvent_t vrEvent) { }
        protected virtual void onReleaseOculusGrip(ref VREvent_t vrEvent) { }
        protected virtual void onReleaseOculusAX(ref VREvent_t vrEvent) { }
        protected virtual void onReleaseOculusBY(ref VREvent_t vrEvent) { }
        protected virtual void onUntouchOculusStick(ref VREvent_t vrEvent) { }



        /// <summary>
        /// Gives the r-theta parameterization of the point on the touchpad that
        /// the user is touching.
        /// </summary>
        /// <param name="deviceIndex">The index of the controller</param>
        /// <param name="r">the radius [0,1]</param>
        /// <param name="theta">the angle made with the x-axis</param>
        protected void getViveTouchpadPoint(uint deviceIndex, out float r, out float theta)
        {
            VRControllerState_t controllerState = new VRControllerState_t();
            VRControllerAxis_t axis = new VRControllerAxis_t();
            unsafe
            {
                mScene.mHMD.GetControllerState(deviceIndex, ref controllerState, (uint)sizeof(VRControllerState_t));
            }
            axis = controllerState.rAxis0;
            r = (float)Math.Sqrt(axis.x * axis.x + axis.y * axis.y);
            if (r > 0)
                theta = (float)Math.Atan2(axis.y / r, axis.x / r); // TODO: Remove divisions?
            else
                theta = 0;
        }

        /// <summary>
        /// Gives the r-theta parameterization of position of the joystick
        /// on the Oculus Touch controller.
        /// </summary>
        /// <param name="deviceIndex">Index of the controller</param>
        /// <param name="r">the tilt magnitude [0,1]</param>
        /// <param name="theta">the angle made with the x-axis</param>
        protected void getOculusJoystickPoint(uint deviceIndex, out float r, out float theta)
        {
            getViveTouchpadPoint(deviceIndex, out r, out theta);
        }
    }
}
