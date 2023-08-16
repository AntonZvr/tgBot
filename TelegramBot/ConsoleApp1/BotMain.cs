using System;

class BotMain
{
    private HttpClient httpClient;
    static void Main()
    {
        BotLogic botLogic = new BotLogic();
        botLogic.InitializeBot();
    }
}
