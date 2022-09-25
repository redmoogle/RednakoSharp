using Discord;

namespace RednakoSharp.Helpers
{
    public sealed class SelectorComponents
    {
        public static MessageComponent AnimalMenu(string menuid)
        {
            ComponentBuilder builder = new();
            SelectMenuBuilder selector = new();
            selector.WithMaxValues(1);
            selector.WithMinValues(1);
            selector.Placeholder = "Select or Perish";
            selector.AddOption("Random", "random", "Surprise!", emote: new Emoji("\U0001F3B2"));
            selector.AddOption("Cat", "cat", "Meow", emote: new Emoji("\U0001F408"));
            selector.AddOption("Dog", "dog", "Woof", emote: new Emoji("\U0001F436"));
            selector.AddOption("Fox", "fox", "Honk", emote: new Emoji("\U0001F98A"));
            selector.AddOption("Bird", "bird", "Chirp", emote: new Emoji("\U0001F426"));
            selector.AddOption("Red Panda", "red_panda", "???", emote: new Emoji("\U0001F534"));
            selector.AddOption("Panda", "panda", "Yin-Yang", emote: new Emoji("\U0001F43C"));
            selector.AddOption("Koala", "koala", "Demonic Sound Machine", emote: new Emoji("\U0001F428"));
            selector.AddOption("Racoon", "racoon", "Trash Panda", emote: new Emoji("\U0001F99D"));
            selector.AddOption("Kangaroo", "kangaroo", "Oi you fuckin cunt", emote: new Emoji("\U0001F998"));

            selector.WithCustomId(menuid);

            builder.WithSelectMenu(selector);
            return builder.Build();
        }
    }
}
