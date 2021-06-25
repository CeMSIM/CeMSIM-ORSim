﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Controls the metrics UI and tool status UI
/// </summary>
public class LS_UIcontroller : MonoBehaviour
{
    public LSMetricsScoring metricsManager; // Controller for getting the metrics for the simulation
    public GameObject mainMenu;
    public GameObject metricsUI;
    public TMP_InputField subjIDinput;
    public TMP_InputField trialIDinput;
    #region Metrics
    //Total
    public TMP_Text m_totalScore;
    public TMP_Text m_totalCompletionTime;
    public TMP_Text m_Pass;
    //Enterotomy
    public TMP_Text openEnterotomyPoint;
    public TMP_Text secureEnterotomyPoint;
    public TMP_Text m_EnterotomyScore;
    public TMP_Text m_EnterotomyTime;
    //LS-Insertion
    public TMP_Text insertionCloseLS;
    public TMP_Text secureInsertionOpening;
    public TMP_Text m_LSInsertionScore;
    public TMP_Text m_LSInsertionTime;
    //Stapled-Anastomosis
    public TMP_Text fullStapling;
    public TMP_Text LSOpenRemove;
    public TMP_Text m_StapledAnastScore;
    public TMP_Text m_StapledAnastTime;
    //Final-Closure
    public TMP_Text openingSecured;
    public TMP_Text openingFullyGrasped;
    public TMP_Text cutZoneCrossed;
    public TMP_Text closureCloseLS;
    public TMP_Text fullyStapling;
    public TMP_Text mesenteryClear;
    public TMP_Text m_FinalClosureScore;
    public TMP_Text m_FinalClosureTime;
    #endregion

    /// <summary>
    /// Store user entered subject ID and trial ID on the start menu
    /// </summary>
    public void StoreID()
    {
        if (string.IsNullOrWhiteSpace(subjIDinput.text) || string.IsNullOrWhiteSpace(trialIDinput.text))
        {
            return;
        }

        LSSimDataRecording.subjID = subjIDinput.text;
        LSSimDataRecording.trialID = trialIDinput.text;

        globalOperators.SimStart();
        mainMenu.SetActive(false); // Hide main menu
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            metricsUI.SetActive(!metricsUI.activeSelf);
        }

        m_totalScore.text = "  Score " + metricsManager.m_totalScore;
        m_totalCompletionTime.text = "  Time " + metricsManager.m_totalCompletionTime.ToString("N3");
        openEnterotomyPoint.text = "  Open Enterotomy Point " + metricsManager.m_EnterotomyMetricsScores["OpenEnterotomyPoint"];
        secureEnterotomyPoint.text = "  Secure Enterotomy Point " + metricsManager.m_EnterotomyMetricsScores["SecureEnterotomyPoint"];
        m_EnterotomyScore.text = "  Score " + metricsManager.m_EnterotomyScore;
        m_EnterotomyTime.text = "  Time " + metricsManager.m_EnterotomyTime.ToString("N3");
        insertionCloseLS.text = "  Close LS " + metricsManager.m_LSInsertionMetricsScores["CloseLS"];
        secureInsertionOpening.text = "  Secure Insertion Opening " + metricsManager.m_LSInsertionMetricsScores["SecureInsertionOpening"];
        m_LSInsertionScore.text = "  Score " + metricsManager.m_LSInsertionScore;
        m_LSInsertionTime.text = "  Time " + metricsManager.m_LSInsertionTime.ToString("N3");
        fullStapling.text = "  Full Stapling " + metricsManager.m_StapledAnastMetricsScores["FullStapling"];
        LSOpenRemove.text = "  LS Open Remove " + metricsManager.m_StapledAnastMetricsScores["LSOpenRemove"];
        m_StapledAnastScore.text = "  Score " + metricsManager.m_StapledAnastScore;
        m_StapledAnastTime.text = "  Time " + metricsManager.m_StapledAnastTime.ToString("N3");
        openingSecured.text = "  Opening Secured " + metricsManager.m_FinalClosureMetricsScores["OpeningSecured"];
        openingFullyGrasped.text = "  Opening Fully Grasped " + metricsManager.m_FinalClosureMetricsScores["OpeningFullyGrasped"];
        cutZoneCrossed.text = "  Cut Zone Crossed " + metricsManager.m_FinalClosureMetricsScores["CutZoneCrossed"];
        closureCloseLS.text = "  Close LS " + metricsManager.m_FinalClosureMetricsScores["CloseLS"];
        fullyStapling.text = "  Fully Stapling " + metricsManager.m_FinalClosureMetricsScores["FullyStapling"];
        mesenteryClear.text = "  Mesentery Clear " + metricsManager.m_FinalClosureMetricsScores["MesenteryClear"];
        m_FinalClosureScore.text = "  Score " + metricsManager.m_FinalClosureScore;
        m_FinalClosureTime.text = "  Time " + metricsManager.m_FinalClosureTime.ToString("N3");

        if (metricsManager.m_bFinalResultEvaluated)
        {
            m_Pass.text = metricsManager.m_bPass ? "Pass" : "Fail";
        }
    }
}