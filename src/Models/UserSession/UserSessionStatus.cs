internal enum UserSessionStatus
{
    None = 0,
    FillingQuestions = 1,
    BackingToPreviousQuestion = 2,
    UserSendingUsernameBlacklist = 3,
    SendingFirm = 4,
    AdminWaitingBanUsername = 5,
    AdminWaitingKickUsername = 6,
    AdminWaitingNewstellerMessage = 7,
    UserFillingDataReportChecker = 8,
    AdminWaitingLimitsUsername = 9
}
