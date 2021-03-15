﻿using System.Collections;
using System.Collections.Generic;
using RosSharp.RosBridgeClient;
using RosSharp.RosBridgeClient.MessageTypes.RoboyMiddleware;
using UnityEngine;
using Widgets;

public class CageSubscriber : UnitySubscriber<ExoforceResponse>
{
    [SerializeField] private bool isInit;

    protected override void ReceiveMessage(ExoforceResponse message)
    {
        Debug.Log(message.success + ": Received " + message.message);
        if (message.success)
        {
            CageInterface.cageIsConnected = isInit;
            var newIcon = "";
            if (isInit)
            {
                newIcon = "Cage";
                CageInterface.sentInitRequest = false;
            }
            else
            {
                newIcon = "CageRed";
            }
            // turn the icon to the corresponding icon
            Widget cageWidget = Manager.Instance.FindWidgetWithID(60);
            //var context = cageWidget.GetContext();
            cageWidget.GetContext().currentIcon = newIcon;
            print("context.currentIcon" + cageWidget.GetContext().currentIcon);
            //cageWidget.ProcessRosMessage(cageWidget.GetContext());
        }
        else
        {
            if (isInit)
            {
                CageInterface.sentInitRequest = false;
            }
            Debug.LogWarning("Request to Cage unssuccessfull: " + message.message);
        }
    }
}
