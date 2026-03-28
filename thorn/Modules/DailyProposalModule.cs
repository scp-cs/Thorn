using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using thorn.Services;

namespace thorn.Modules;

public class DailyProposalModule(GitHubService github) : InteractionModuleBase<SocketInteractionContext>
{
    private static readonly int[] DaysInMonth = [31, 29, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];

    [SlashCommand("navrhnout-daily", "Navrhni zajímavost do ranního přehledu")]
    public async Task ProposeDaily(
        [Summary("den", "Den v měsíci (1-31)")] int day,
        [Summary("měsíc", "Měsíc (1-12)")] int month,
        [Summary("rok", "Rok události (např. 1989)")] string year,
        [Summary("událost", "Popis události")] string eventText)
    {
        if (month < 1 || month > 12 || day < 1 || day > DaysInMonth[month - 1])
        {
            await RespondAsync("Neplatné datum!", ephemeral: true);
            return;
        }

        if (int.TryParse(year, out var yearNum))
        {
            var eventDate = new DateTime(yearNum, month, day);
            if (eventDate > DateTime.Today)
            {
                await RespondAsync("Událost nemůže být v budoucnosti!", ephemeral: true);
                return;
            }
        }

        if (github.IsRateLimited(Context.User.Id))
        {
            await RespondAsync("Dnes už nemůžeš vytvořit další návrh :(", ephemeral: true);
            return;
        }

        await DeferAsync();
        
        eventText = char.ToUpper(eventText[0]) + eventText[1..];
        if (!eventText.EndsWith('.'))
            eventText += '.';

        var date = $"{day:D2} {month:D2}";
        var prUrl = await github.CreateProposalPrAsync(date, year, eventText, Context.User);

        if (prUrl != null)
            await FollowupAsync($"Návrh vytvořen! {prUrl}", flags: MessageFlags.SuppressEmbeds);
        else
            await FollowupAsync("Nepodařilo se vytvořit návrh :(", ephemeral: true);
    }
}
