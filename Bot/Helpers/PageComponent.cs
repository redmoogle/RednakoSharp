using Discord;
using Discord.Interactions;
using System.Globalization;
using System.Text;

namespace RednakoSharp.Helpers
{
    // Not sealed to allow for custom behaviour

    /// <summary>
    /// Create a component that contains pages (10 items per page by default)
    /// </summary>
    public class PageComponent
    {
        private static readonly Emoji DecrementEmoji = new("\U000025C0");
        private static readonly Emoji IncrementEmoji = new("\U000025B6");

        /// <summary>
        /// What page were on
        /// </summary>
        public int CurrentPage { get; internal set; }
        /// <summary>
        /// How many items per page
        /// </summary>
        public int PerPage { get; } = 10;

        /// <summary>
        /// Contained Items
        /// </summary>

        public readonly IEnumerable<object> Items;

        private readonly SocketInteractionContext _context;

        public PageComponent(IEnumerable<object> items, SocketInteractionContext context)
        {
            this.Items = items;
            this._context = context;
            
            Tuple<Embed, MessageComponent> message = GetPageComponent();
            _context.Interaction.RespondAsync(embeds: new[] { message.Item1 }, components: message.Item2 ).GetAwaiter().GetResult();
        }

        public PageComponent(IEnumerable<object> items, SocketInteractionContext context, int perPage) {
            this.Items = items;
            this._context = context;
            this.PerPage = perPage;

            Tuple<Embed, MessageComponent> message = GetPageComponent();
            _context.Interaction.RespondAsync(embeds: new[] { message.Item1 }, components: message.Item2 ).GetAwaiter().GetResult();
        }

        public int TotalPages()
        {
            // So when dividing two integers you get the floor, however I need the ceiling
            return (Items.Count() + (PerPage - 1))/PerPage;
        }

        internal Tuple<Embed, MessageComponent> GetPageComponent()
        {
            IEnumerable<object> pageItems = Items.Take(new Range(CurrentPage*PerPage, CurrentPage*PerPage+PerPage));

            ComponentBuilder componentBuilder = new();
            MessageComponent component;

            if(CurrentPage+1 != TotalPages())
            {
                componentBuilder.WithButton(emote: IncrementEmoji, customId: "increase");
            }

            if(CurrentPage != 0)
            {
                componentBuilder.WithButton(emote: DecrementEmoji, customId: "decrease");
            }

            component = componentBuilder.Build();

            EmbedBuilder embedBuilder = new();
            StringBuilder stringBuilder = new();

            stringBuilder.AppendLine("```");

            int index = 0;
            foreach (object item in pageItems)
            {
                index++;
                stringBuilder.AppendLine(index + ": " + item.ToString());
            }
            stringBuilder.AppendLine("```");

            embedBuilder.WithDescription(stringBuilder.ToString());
            embedBuilder.Footer = new EmbedFooterBuilder().WithText((CurrentPage+1).ToString(CultureInfo.InvariantCulture) + "/" + TotalPages().ToString(CultureInfo.InvariantCulture));
            
            return new Tuple<Embed, MessageComponent>(embedBuilder.Build(), component);

        }

        public Tuple<Embed, MessageComponent> IncrementPage()
        {
            if(CurrentPage < TotalPages())
            {
                CurrentPage++;
            }
            return GetPageComponent();
        }

        public Tuple<Embed, MessageComponent> DecrementPage()
        {
            if(CurrentPage > 1) {
                CurrentPage--;
            }
            return GetPageComponent();
        }

        public Tuple<Embed, MessageComponent> SetPage(int page)
        {
            if (CurrentPage < TotalPages() && CurrentPage > 0)
            {
                CurrentPage = page;
            }
            return GetPageComponent();
        }

        [ComponentInteraction("decrease")]
        public void DecrementTask()
        {
            Tuple<Embed, MessageComponent> returnable = DecrementPage();
            _context.Interaction.ModifyOriginalResponseAsync(props => { props.Components = returnable.Item2; props.Embeds = new[] { returnable.Item1 }; }).GetAwaiter().GetResult();
        }

        [ComponentInteraction("increase")]
        public void IncrementTask()
        {
            Tuple<Embed, MessageComponent> returnable = IncrementPage();
            _context.Interaction.ModifyOriginalResponseAsync(props => { props.Components = returnable.Item2; props.Embeds = new[] { returnable.Item1 }; }).GetAwaiter().GetResult();
        }

        public SocketInteractionContext Context => _context;

    }
}