﻿using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ReactiveUI;
using AWBv2.Controls;
using AWBv2.Models;
using Functions;
using Functions.Article;
using ReactiveUI.Fody.Helpers;

namespace AWBv2.ViewModels;

public class MainWindowViewModel : ReactiveObject
{
    private AWBWebBrowser _webBrowser;
    [Reactive] public bool IsMinorEdit { get; set; } = false;
    [Reactive] public string LblUsername { get; set; } = string.Empty;
    [Reactive] public string LblProject { get; set; } = string.Empty;
    [Reactive] public int LblNewArticles { get; set; } = 0;
    [Reactive] public int LblIgnoredArticles { get; set; } = 0;
    [Reactive] public int LblEditCount { get; set; } = 0;
    [Reactive] public int LblEditsPerMin { get; set; } = 0;
    [Reactive] public int LblPagesPerMin { get; set; } = 0;
    [Reactive] public int LblTimer { get; set; } = 0;

    [Reactive] public ObservableCollection<Article> Articles { get; set; } = new();
    
    private Wiki Wiki { get; set; }
    
    public ReactiveCommand<Unit, Unit> OpenProfileWindow { get; }
    public ReactiveCommand<Unit, Unit> RequestClose { get; }
    public Interaction<ProfileWindowViewModel, Unit> ShowProfileWindowInteraction { get; }
    public Interaction<Unit, Unit> CloseWindowInteraction { get; }

    public AWBWebBrowser WebBrowser
    {
        get => _webBrowser ??= new AWBWebBrowser();
        set => this.RaiseAndSetIfChanged(ref _webBrowser, value);
    }
    
    [Reactive] public MakeListViewModel MakeListViewModel { get; set; }
    
    [Reactive] public ProcessOptionsViewModel ProcessOptionsViewModel { get; set; }
    
    public ReactiveCommand<Unit, Unit> ProcessArticlesCommand { get; }
    
    public MainWindowViewModel()
    {
        ShowProfileWindowInteraction = new Interaction<ProfileWindowViewModel, Unit>();
        CloseWindowInteraction = new Interaction<Unit, Unit>();

        OpenProfileWindow = ReactiveCommand.CreateFromTask(ShowProfileWindow);
        RequestClose = ReactiveCommand.CreateFromTask(CloseWindow);
        MakeListViewModel = new MakeListViewModel();
        ProcessOptionsViewModel = new ProcessOptionsViewModel();
        ProcessArticlesCommand = ReactiveCommand.CreateFromTask(ProcessArticlesAsync);
    }

    private async Task CloseWindow()
    {
        await CloseWindowInteraction.Handle(Unit.Default);
    }

    private async Task ShowProfileWindow()
    {
        var profileVM = new ProfileWindowViewModel();
        await ShowProfileWindowInteraction.Handle(profileVM);
    }
    
    public async Task HandleProfileLogin(Profile profile)
    {
        
        try
        {
            Wiki = await Wiki.CreateAsync(profile.Wiki);
            await Wiki.ApiClient.LoginUserAsync(profile.Username, profile.Password);
            await Wiki.ApiClient.FetchUserInformationAsync();
            
            // deeeeeeebug
            Console.WriteLine(JsonSerializer.Serialize(Wiki.User, new JsonSerializerOptions { WriteIndented = true }));
            MakeListViewModel.Initialize(Wiki);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to init wiki: {ex.Message}");
            throw;
        }

        LblProject = Wiki.Sitename;
        LblUsername = Wiki.User.Username;
    }

    private async Task ProcessArticlesAsync()
    {
        foreach (string pageTitle in MakeListViewModel.Pages)
        {
            try
            {
                var article = await Wiki.ApiClient.GetArticleAsync(pageTitle);
                
                if (article != null)
                {
                    Articles.Add(article);
                
                    // Print to console as dehbug for naw
                    // @TODO: remove plz
                    Console.WriteLine($"Title: {article.Name}");
                    Console.WriteLine($"Content: {article.OriginalArticleText}");
                    Console.WriteLine($"Protected: {article.Protections.Any()}");
                }
                else
                {
                    ++LblIgnoredArticles;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing {pageTitle}: {ex.Message}");
            }

        }
    }
}
