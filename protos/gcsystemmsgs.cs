//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
#pragma warning disable 1591

// Generated from: gcsystemmsgs.proto
namespace nora.protos
{
    public enum EGCSystemMsg
    {
            
      k_EGCMsgInvalid = 0,
            
      k_EGCMsgMulti = 1,
            
      k_EGCMsgGenericReply = 10,
            
      k_EGCMsgSystemBase = 50,
            
      k_EGCMsgAchievementAwarded = 51,
            
      k_EGCMsgConCommand = 52,
            
      k_EGCMsgStartPlaying = 53,
            
      k_EGCMsgStopPlaying = 54,
            
      k_EGCMsgStartGameserver = 55,
            
      k_EGCMsgStopGameserver = 56,
            
      k_EGCMsgWGRequest = 57,
            
      k_EGCMsgWGResponse = 58,
            
      k_EGCMsgGetUserGameStatsSchema = 59,
            
      k_EGCMsgGetUserGameStatsSchemaResponse = 60,
            
      k_EGCMsgGetUserStatsDEPRECATED = 61,
            
      k_EGCMsgGetUserStatsResponse = 62,
            
      k_EGCMsgAppInfoUpdated = 63,
            
      k_EGCMsgValidateSession = 64,
            
      k_EGCMsgValidateSessionResponse = 65,
            
      k_EGCMsgLookupAccountFromInput = 66,
            
      k_EGCMsgSendHTTPRequest = 67,
            
      k_EGCMsgSendHTTPRequestResponse = 68,
            
      k_EGCMsgPreTestSetup = 69,
            
      k_EGCMsgRecordSupportAction = 70,
            
      k_EGCMsgGetAccountDetails_DEPRECATED = 71,
            
      k_EGCMsgReceiveInterAppMessage = 73,
            
      k_EGCMsgFindAccounts = 74,
            
      k_EGCMsgPostAlert = 75,
            
      k_EGCMsgGetLicenses = 76,
            
      k_EGCMsgGetUserStats = 77,
            
      k_EGCMsgGetCommands = 78,
            
      k_EGCMsgGetCommandsResponse = 79,
            
      k_EGCMsgAddFreeLicense = 80,
            
      k_EGCMsgAddFreeLicenseResponse = 81,
            
      k_EGCMsgGetIPLocation = 82,
            
      k_EGCMsgGetIPLocationResponse = 83,
            
      k_EGCMsgSystemStatsSchema = 84,
            
      k_EGCMsgGetSystemStats = 85,
            
      k_EGCMsgGetSystemStatsResponse = 86,
            
      k_EGCMsgSendEmail = 87,
            
      k_EGCMsgSendEmailResponse = 88,
            
      k_EGCMsgGetEmailTemplate = 89,
            
      k_EGCMsgGetEmailTemplateResponse = 90,
            
      k_EGCMsgGrantGuestPass = 91,
            
      k_EGCMsgGrantGuestPassResponse = 92,
            
      k_EGCMsgGetAccountDetails = 93,
            
      k_EGCMsgGetAccountDetailsResponse = 94,
            
      k_EGCMsgGetPersonaNames = 95,
            
      k_EGCMsgGetPersonaNamesResponse = 96,
            
      k_EGCMsgMultiplexMsg = 97,
            
      k_EGCMsgWebAPIRegisterInterfaces = 101,
            
      k_EGCMsgWebAPIJobRequest = 102,
            
      k_EGCMsgWebAPIJobRequestHttpResponse = 104,
            
      k_EGCMsgWebAPIJobRequestForwardResponse = 105,
            
      k_EGCMsgMemCachedGet = 200,
            
      k_EGCMsgMemCachedGetResponse = 201,
            
      k_EGCMsgMemCachedSet = 202,
            
      k_EGCMsgMemCachedDelete = 203,
            
      k_EGCMsgMemCachedStats = 204,
            
      k_EGCMsgMemCachedStatsResponse = 205,
            
      k_EGCMsgSQLStats = 210,
            
      k_EGCMsgSQLStatsResponse = 211,
            
      k_EGCMsgMasterSetDirectory = 220,
            
      k_EGCMsgMasterSetDirectoryResponse = 221,
            
      k_EGCMsgMasterSetWebAPIRouting = 222,
            
      k_EGCMsgMasterSetWebAPIRoutingResponse = 223,
            
      k_EGCMsgMasterSetClientMsgRouting = 224,
            
      k_EGCMsgMasterSetClientMsgRoutingResponse = 225,
            
      k_EGCMsgSetOptions = 226,
            
      k_EGCMsgSetOptionsResponse = 227,
            
      k_EGCMsgSystemBase2 = 500,
            
      k_EGCMsgGetPurchaseTrustStatus = 501,
            
      k_EGCMsgGetPurchaseTrustStatusResponse = 502,
            
      k_EGCMsgUpdateSession = 503,
            
      k_EGCMsgGCAccountVacStatusChange = 504,
            
      k_EGCMsgCheckFriendship = 505,
            
      k_EGCMsgCheckFriendshipResponse = 506,
            
      k_EGCMsgGetPartnerAccountLink = 507,
            
      k_EGCMsgGetPartnerAccountLinkResponse = 508,
            
      k_EGCMsgVSReportedSuspiciousActivity = 509,
            
      k_EGCMsgDPPartnerMicroTxns = 512,
            
      k_EGCMsgDPPartnerMicroTxnsResponse = 513
    }
  
    public enum ESOMsg
    {
            
      k_ESOMsg_Create = 21,
            
      k_ESOMsg_Update = 22,
            
      k_ESOMsg_Destroy = 23,
            
      k_ESOMsg_CacheSubscribed = 24,
            
      k_ESOMsg_CacheUnsubscribed = 25,
            
      k_ESOMsg_UpdateMultiple = 26,
            
      k_ESOMsg_CacheSubscriptionRefresh = 28,
            
      k_ESOMsg_CacheSubscribedUpToDate = 29
    }
  
    public enum EGCBaseClientMsg
    {
            
      k_EMsgGCPingRequest = 3001,
            
      k_EMsgGCPingResponse = 3002,
            
      k_EMsgGCClientWelcome = 4004,
            
      k_EMsgGCServerWelcome = 4005,
            
      k_EMsgGCClientHello = 4006,
            
      k_EMsgGCServerHello = 4007,
            
      k_EMsgGCClientConnectionStatus = 4009,
            
      k_EMsgGCServerConnectionStatus = 4010
    }
  
    public enum EGCToGCMsg
    {
            
      k_EGCToGCMsgMasterAck = 150,
            
      k_EGCToGCMsgMasterAckResponse = 151,
            
      k_EGCToGCMsgRouted = 152,
            
      k_EGCToGCMsgRoutedReply = 153,
            
      k_EMsgGCUpdateSubGCSessionInfo = 154,
            
      k_EMsgGCRequestSubGCSessionInfo = 155,
            
      k_EMsgGCRequestSubGCSessionInfoResponse = 156,
            
      k_EGCToGCMsgMasterStartupComplete = 157,
            
      k_EMsgGCToGCSOCacheSubscribe = 158,
            
      k_EMsgGCToGCSOCacheUnsubscribe = 159,
            
      k_EMsgGCToGCLoadSessionSOCache = 160,
            
      k_EMsgGCToGCLoadSessionSOCacheResponse = 161,
            
      k_EMsgGCToGCUpdateSessionStats = 162
    }
  
}
#pragma warning restore 1591
