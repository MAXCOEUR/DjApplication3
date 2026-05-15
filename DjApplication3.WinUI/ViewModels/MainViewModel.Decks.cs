using DjApplication3.model;
using System;
using System.ComponentModel;
using System.Linq;

namespace DjApplication3.WinUI.ViewModels
{
    public sealed partial class MainViewModel
    {
        public void UpdateDeckHeights(double availableHeight)
        {
            if (Decks.Count == 0 || double.IsNaN(availableHeight) || availableHeight <= 0)
            {
                return;
            }

            const double minimumDeckHeight = 250;
            var spacing = Math.Max(0, Decks.Count - 1) * 10;
            var targetHeight = Math.Max(minimumDeckHeight, (availableHeight - spacing) / Decks.Count);
            foreach (var deck in Decks)
            {
                deck.DeckHeight = targetHeight;
            }
        }

        private void RefreshDecks()
        {
            while (Decks.Count > TrackCount)
            {
                var deck = Decks[^1];
                deck.PropertyChanged -= Deck_PropertyChanged;
                deck.BpmCalculated -= Deck_BpmCalculated;
                deck.Dispose();
                Decks.RemoveAt(Decks.Count - 1);
            }

            while (Decks.Count < TrackCount)
            {
                var deck = new DeckViewModel(Decks.Count + 1, _library, _settings, _dispatcherQueue);
                deck.PropertyChanged += Deck_PropertyChanged;
                deck.BpmCalculated += Deck_BpmCalculated;
                Decks.Add(deck);
            }

            TrackNumbers.Clear();
            for (var i = 1; i <= TrackCount; i++)
            {
                TrackNumbers.Add(i);
            }

            LeftDeckNumber = Math.Clamp(LeftDeckNumber, 1, TrackCount);
            RightDeckNumber = Math.Clamp(RightDeckNumber, 1, TrackCount);
            LeftDeckIndex = LeftDeckNumber - 1;
            RightDeckIndex = RightDeckNumber - 1;
            ApplyCrossfade();
            SyncControllerState();
        }

        private void Deck_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(DeckViewModel.IsPlaying)
                or nameof(DeckViewModel.IsHeadphone)
                or nameof(DeckViewModel.HasMusic))
            {
                SyncControllerState();
            }
        }

        private void Deck_BpmCalculated(object? sender, int bpm)
        {
            if (sender is not DeckViewModel deck) return;

            foreach (var row in Musics.Where(row => MusicIdentity.SameTrack(row.Musique, deck.CurrentMusic)))
            {
                row.Bpm = bpm;
            }
        }

        private void ApplyCrossfade()
        {
            if (Decks.Count < 2) return;
            var leftVolume = Crossfade <= 50 ? 1 : 1 - ((Crossfade - 50) / 50.0f);
            var rightVolume = Crossfade >= 50 ? 1 : Crossfade / 50.0f;
            if (LeftDeckIndex >= 0 && LeftDeckIndex < Decks.Count) Decks[LeftDeckIndex].SetMasterVolume(leftVolume);
            if (RightDeckIndex >= 0 && RightDeckIndex < Decks.Count) Decks[RightDeckIndex].SetMasterVolume(rightVolume);
        }
    }
}
