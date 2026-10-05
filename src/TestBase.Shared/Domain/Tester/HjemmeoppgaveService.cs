using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Administrasjon;

namespace TestBase.Shared.Domain.Tester;

/// <summary>Ett ledd i en hjemmeoppgave slik forfatteren (behandleren) har bygget den i editoren.</summary>
public sealed record HjemmeoppgaveLeddInput(
    string Sporsmalstekst, string? Instruksjon, TestSvartype Svartype, string? Svaralternativer,
    bool ErPaakrevd, string? BildeData, string? BildeContentType, string? BildeUrl = null);

public enum HjemmeoppgaveSlettResultat { IkkeFunnet, IngenTilgang, Slettet, ArkivertIStedet }

/// <summary>
/// Hjemmeoppgaver (2026-10-04, se docs/beslutningslogg.md "Hjemmeoppgaver og programmer") —
/// behandler-forfattede tester, ATSKILT fra TestService sin admin-forfatning
/// (Admin/Tester/Sider+Ledd) fordi: (1) eierskap/deling er et helt nytt konsept TestService sine
/// metoder ikke kjenner til, (2) TestService.OpprettTestAsync gir ALLE partnere automatisk
/// PartnerTestTilgang — riktig for admin-forfattede tester (ment for bred distribusjon), men
/// GALT for en hjemmeoppgave som skal starte PRIVAT og kun bli synlig for andre når eieren
/// eksplisitt deler den, se OpprettAsync som derfor IKKE kaller TestService sin metode.
/// </summary>
public sealed class HjemmeoppgaveService
{
    private readonly AppDbContext _db;

    public HjemmeoppgaveService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Test> OpprettAsync(
        long behandlerId, string navn, string? beskrivelse, string? belonningsTittel, string? belonningstekst,
        IReadOnlyList<HjemmeoppgaveLeddInput> ledd, CancellationToken cancellationToken = default)
    {
        var test = new Test
        {
            Navn = navn,
            Beskrivelse = beskrivelse,
            BelonningsTittel = belonningsTittel,
            Belonningstekst = belonningstekst,
            ErHjemmeoppgave = true,
            OpprettetAvBehandlerId = behandlerId,
            ErAktiv = true,
            OpprettetUtc = DateTimeOffset.UtcNow
        };
        _db.Tester.Add(test);
        await _db.SaveChangesAsync(cancellationToken);

        // Bevisst ÉN fast side (bugliste: "flat liste, for nå") — samme TestSide/TestLedd-modell
        // som admin-forfattede tester bruker, bare at hjemmeoppgave-editoren aldri eksponerer
        // sidekonseptet til forfatteren.
        var side = new TestSide { TestId = test.Id, Navn = "Oppgave", Rekkefolge = 1 };
        _db.TestSider.Add(side);
        await _db.SaveChangesAsync(cancellationToken);

        await LeggTilLeddRaderAsync(side.Id, ledd, cancellationToken);
        return test;
    }

    private async Task LeggTilLeddRaderAsync(long testSideId, IReadOnlyList<HjemmeoppgaveLeddInput> ledd, CancellationToken cancellationToken)
    {
        var rekkefolge = 1;
        foreach (var input in ledd)
        {
            var erBilde = input.Svartype == TestSvartype.Bilde;
            _db.TestLedd.Add(new TestLedd
            {
                TestSideId = testSideId,
                Rekkefolge = rekkefolge++,
                Sporsmalstekst = input.Sporsmalstekst,
                Instruksjon = input.Instruksjon,
                Svartype = input.Svartype,
                Svaralternativer = input.Svaralternativer,
                // Et Bilde-ledd er rent visningsinnhold og kan ikke "besvares" — ALDRI påkrevd,
                // uansett hva editoren måtte ha sendt inn (forsvar i dybden, se TestLedd.ErPaakrevd).
                ErPaakrevd = !erBilde && input.ErPaakrevd,
                BildeData = erBilde ? input.BildeData : null,
                BildeContentType = erBilde ? input.BildeContentType : null,
                BildeUrl = erBilde ? input.BildeUrl : null
            });
        }
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Metadata (navn/beskrivelse/belønning) kan ALLTID redigeres. Selve ledd-listen kan KUN
    /// erstattes så lenge INGEN pasient ennå er tildelt denne hjemmeoppgaven — en strukturell
    /// endring etterpå ville foreldreløsgjort/slettet eksisterende TestSvar-rader (samme
    /// forsiktighetsprinsipp som TestService.SlettTestHeltForRegenereringAsync sin advarsel: KUN
    /// for tester under aktiv iterasjon, ALDRI for en test med ekte svar). Returnerer false (ingen
    /// endring gjort) hvis chooser prøver å endre ledd-listen på en allerede tildelt hjemmeoppgave
    /// — kalleren bør da anbefale "Kopier" i stedet.
    /// </summary>
    public async Task<(bool Lykkes, bool LeddLaast, string? Feilmelding)> OppdaterAsync(
        long testId, long behandlerId, string navn, string? beskrivelse, string? belonningsTittel, string? belonningstekst,
        IReadOnlyList<HjemmeoppgaveLeddInput> ledd, CancellationToken cancellationToken = default)
    {
        var test = await _db.Tester.FirstOrDefaultAsync(t => t.Id == testId && t.ErHjemmeoppgave, cancellationToken);
        if (test is null || test.OpprettetAvBehandlerId != behandlerId)
        {
            return (false, false, "Fant ikke hjemmeoppgaven, eller du eier den ikke.");
        }

        test.Navn = navn;
        test.Beskrivelse = beskrivelse;
        test.BelonningsTittel = belonningsTittel;
        test.Belonningstekst = belonningstekst;

        var side = await _db.TestSider.FirstOrDefaultAsync(s => s.TestId == testId, cancellationToken);
        if (side is null)
        {
            await _db.SaveChangesAsync(cancellationToken);
            return (true, false, null);
        }

        var harTildelinger = await _db.TestTildelinger.AnyAsync(t => t.TestId == testId, cancellationToken);
        if (harTildelinger)
        {
            // Kun metadata lagret over — ledd-listen rørt IKKE.
            await _db.SaveChangesAsync(cancellationToken);
            return (true, true, "Denne hjemmeoppgaven er allerede tildelt minst én pasient, så selve " +
                "spørsmålslisten kan ikke endres (ville ødelagt eksisterende svar). Tittel/forklaring/" +
                "takk-tekst er likevel lagret. Bruk \"Kopier\" for å lage en ny versjon med endrede spørsmål.");
        }

        var eksisterendeLedd = await _db.TestLedd.Where(l => l.TestSideId == side.Id).ToListAsync(cancellationToken);
        _db.TestLedd.RemoveRange(eksisterendeLedd);
        await _db.SaveChangesAsync(cancellationToken);
        await LeggTilLeddRaderAsync(side.Id, ledd, cancellationToken);

        return (true, false, null);
    }

    public async Task<HjemmeoppgaveSlettResultat> SlettAsync(long testId, long behandlerId, CancellationToken cancellationToken = default)
    {
        var test = await _db.Tester.FirstOrDefaultAsync(t => t.Id == testId && t.ErHjemmeoppgave, cancellationToken);
        if (test is null)
        {
            return HjemmeoppgaveSlettResultat.IkkeFunnet;
        }
        if (test.OpprettetAvBehandlerId != behandlerId)
        {
            return HjemmeoppgaveSlettResultat.IngenTilgang;
        }

        var harTildelinger = await _db.TestTildelinger.AnyAsync(t => t.TestId == testId, cancellationToken);
        if (harTildelinger)
        {
            // Ekte svar finnes et sted i systemet — ALDRI hard-slett, arkiver (ErAktiv=false) i
            // stedet, samme prinsipp som OppdaterAsync sin ledd-låsing over.
            test.ErAktiv = false;
            await _db.SaveChangesAsync(cancellationToken);
            return HjemmeoppgaveSlettResultat.ArkivertIStedet;
        }

        var sider = await _db.TestSider.Where(s => s.TestId == testId).ToListAsync(cancellationToken);
        var sideIder = sider.Select(s => s.Id).ToList();
        _db.TestLedd.RemoveRange(_db.TestLedd.Where(l => sideIder.Contains(l.TestSideId)));
        _db.TestSider.RemoveRange(sider);
        _db.HjemmeoppgaveLikinger.RemoveRange(_db.HjemmeoppgaveLikinger.Where(l => l.TestId == testId));
        _db.Tester.Remove(test);
        await _db.SaveChangesAsync(cancellationToken);
        return HjemmeoppgaveSlettResultat.Slettet;
    }

    /// <summary>Oppretter en full, uavhengig kopi — ALDRI en referanse. Starter alltid PRIVAT (ingen deling videreført), se Test.KopiertFraTestId.</summary>
    public async Task<Test?> KopierAsync(long testId, long behandlerId, CancellationToken cancellationToken = default)
    {
        var original = await _db.Tester.FirstOrDefaultAsync(t => t.Id == testId && t.ErHjemmeoppgave, cancellationToken);
        if (original is null)
        {
            return null;
        }

        var originalSide = await _db.TestSider.FirstOrDefaultAsync(s => s.TestId == testId, cancellationToken);
        var originalLedd = originalSide is null
            ? new List<TestLedd>()
            : await _db.TestLedd.Where(l => l.TestSideId == originalSide.Id).OrderBy(l => l.Rekkefolge).ToListAsync(cancellationToken);

        var kopi = new Test
        {
            Navn = $"Kopi av {original.Navn}",
            Beskrivelse = original.Beskrivelse,
            BelonningsTittel = original.BelonningsTittel,
            Belonningstekst = original.Belonningstekst,
            ErHjemmeoppgave = true,
            OpprettetAvBehandlerId = behandlerId,
            KopiertFraTestId = original.Id,
            ErAktiv = true,
            OpprettetUtc = DateTimeOffset.UtcNow
        };
        _db.Tester.Add(kopi);
        await _db.SaveChangesAsync(cancellationToken);

        var kopiSide = new TestSide { TestId = kopi.Id, Navn = "Oppgave", Rekkefolge = 1 };
        _db.TestSider.Add(kopiSide);
        await _db.SaveChangesAsync(cancellationToken);

        await LeggTilLeddRaderAsync(kopiSide.Id, originalLedd.Select(l =>
            new HjemmeoppgaveLeddInput(l.Sporsmalstekst, l.Instruksjon, l.Svartype, l.Svaralternativer, l.ErPaakrevd, l.BildeData, l.BildeContentType, l.BildeUrl)
        ).ToList(), cancellationToken);

        // Hadde behandleren likt originalen fra før — nå har de sin egen kopi i stedet, referansen
        // er redundant (se HjemmeoppgaveLiking sin XML-doc).
        var eksisterendeLiking = await _db.HjemmeoppgaveLikinger
            .FirstOrDefaultAsync(l => l.BehandlerId == behandlerId && l.TestId == testId, cancellationToken);
        if (eksisterendeLiking is not null)
        {
            _db.HjemmeoppgaveLikinger.Remove(eksisterendeLiking);
            await _db.SaveChangesAsync(cancellationToken);
        }

        return kopi;
    }

    /// <summary>Ett klikk, ingen bekreftelse (brukerens eksplisitte svar) — idempotent, en allerede eksisterende liking er en stille no-op.</summary>
    public async Task LikAsync(long behandlerId, long testId, CancellationToken cancellationToken = default)
    {
        var finnesAllerede = await _db.HjemmeoppgaveLikinger.AnyAsync(l => l.BehandlerId == behandlerId && l.TestId == testId, cancellationToken);
        if (finnesAllerede)
        {
            return;
        }
        _db.HjemmeoppgaveLikinger.Add(new HjemmeoppgaveLiking { BehandlerId = behandlerId, TestId = testId, OpprettetUtc = DateTimeOffset.UtcNow });
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Fjerner KUN referansen — rører aldri originalen eller en evt. allerede opprettet kopi (se KopierAsync).</summary>
    public async Task FjernLikingAsync(long behandlerId, long testId, CancellationToken cancellationToken = default)
    {
        var liking = await _db.HjemmeoppgaveLikinger.FirstOrDefaultAsync(l => l.BehandlerId == behandlerId && l.TestId == testId, cancellationToken);
        if (liking is not null)
        {
            _db.HjemmeoppgaveLikinger.Remove(liking);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> SettDeltMedAlleAsync(long testId, long behandlerId, bool verdi, CancellationToken cancellationToken = default)
    {
        var test = await _db.Tester.FirstOrDefaultAsync(t => t.Id == testId && t.ErHjemmeoppgave, cancellationToken);
        if (test is null || test.OpprettetAvBehandlerId != behandlerId)
        {
            return false;
        }
        test.ErDeltMedAlle = verdi;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>Krever at eieren faktisk har en PartnerId (se Behandler.PartnerId) — ellers meningsløst, returnerer false.</summary>
    public async Task<bool> SettDeltMedPartnerAsync(long testId, long behandlerId, bool verdi, CancellationToken cancellationToken = default)
    {
        var test = await _db.Tester.FirstOrDefaultAsync(t => t.Id == testId && t.ErHjemmeoppgave, cancellationToken);
        if (test is null || test.OpprettetAvBehandlerId != behandlerId)
        {
            return false;
        }
        var harPartner = await _db.Behandlere.Where(b => b.Id == behandlerId).Select(b => b.PartnerId).FirstOrDefaultAsync(cancellationToken) is not null;
        if (!harPartner)
        {
            return false;
        }
        test.ErDeltMedPartner = verdi;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>Ledd (i Rekkefolge) for editoren — ingen eierskapssjekk her, kalleren (Rediger.cshtml.cs) har allerede verifisert eierskap via HentPersonligAsync.</summary>
    public async Task<IReadOnlyList<TestLedd>> HentTestLeddForRedigeringAsync(long testId, CancellationToken cancellationToken = default)
    {
        var side = await _db.TestSider.FirstOrDefaultAsync(s => s.TestId == testId, cancellationToken);
        if (side is null)
        {
            return Array.Empty<TestLedd>();
        }
        return await _db.TestLedd.Where(l => l.TestSideId == side.Id).OrderBy(l => l.Rekkefolge).ToListAsync(cancellationToken);
    }

    /// <summary>Hjemmeoppgavene DENNE behandleren selv eier ELLER har likt (referanse) — "Personlig"-fanen.</summary>
    public async Task<IReadOnlyList<Test>> HentPersonligAsync(long behandlerId, CancellationToken cancellationToken = default)
    {
        var likteTestIder = await _db.HjemmeoppgaveLikinger.Where(l => l.BehandlerId == behandlerId).Select(l => l.TestId).ToListAsync(cancellationToken);
        return await _db.Tester
            .Where(t => t.ErHjemmeoppgave && t.ErAktiv && (t.OpprettetAvBehandlerId == behandlerId || likteTestIder.Contains(t.Id)))
            .OrderByDescending(t => t.OpprettetUtc)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Fase 2: alt EN gitt behandler kan tildele (egne + likt + delt-med-alle + partner-delt) —
    /// samme samlede synlighet som Hjemmeoppgaver-sidens tre faner til sammen, brukt til å pinne
    /// en "Egenproduserte"-seksjon øverst i Tildel/Tester sin kategoritre (Behandlerportal KUN —
    /// se HentDeltMedAlleForAdminAsync for Admin sin snevrere variant).
    /// </summary>
    public async Task<IReadOnlyList<Test>> HentTilgjengeligeForTildelingAsync(long behandlerId, CancellationToken cancellationToken = default)
    {
        var personlig = await HentPersonligAsync(behandlerId, cancellationToken);
        var deltMedAlle = await HentDeltMedAlleAsync(behandlerId, cancellationToken);
        var deltMedPartner = await HentDeltMedPartnerAsync(behandlerId, cancellationToken);
        return personlig.Concat(deltMedAlle).Concat(deltMedPartner)
            .GroupBy(t => t.Id).Select(g => g.First())
            .OrderBy(t => t.Navn).ToList();
    }

    /// <summary>
    /// Fase 2: Admin er ikke en behandler (ingen eierskap/partnerskap) — ser derfor KUN
    /// hjemmeoppgaver som er delt med ALLE, aldri andres private/partner-interne hjemmeoppgaver.
    /// </summary>
    public Task<List<Test>> HentDeltMedAlleForAdminAsync(CancellationToken cancellationToken = default) =>
        _db.Tester.Where(t => t.ErHjemmeoppgave && t.ErAktiv && t.ErDeltMedAlle).OrderBy(t => t.Navn).ToListAsync(cancellationToken);

    /// <summary>Delt med ALLE, fra enhver ANNEN behandler (ikke egne — de ligger allerede i "Personlig").</summary>
    public Task<List<Test>> HentDeltMedAlleAsync(long behandlerId, CancellationToken cancellationToken = default) =>
        _db.Tester.Where(t => t.ErHjemmeoppgave && t.ErAktiv && t.ErDeltMedAlle && t.OpprettetAvBehandlerId != behandlerId)
            .OrderByDescending(t => t.OpprettetUtc).ToListAsync(cancellationToken);

    /// <summary>Delt med partneren DENNE behandleren selv tilhører, fra enhver ANNEN behandler i samme partner.</summary>
    public async Task<IReadOnlyList<Test>> HentDeltMedPartnerAsync(long behandlerId, CancellationToken cancellationToken = default)
    {
        var partnerId = await _db.Behandlere.Where(b => b.Id == behandlerId).Select(b => b.PartnerId).FirstOrDefaultAsync(cancellationToken);
        if (partnerId is null)
        {
            return Array.Empty<Test>();
        }
        var partnerBehandlerIder = await _db.Behandlere.Where(b => b.PartnerId == partnerId).Select(b => b.Id).ToListAsync(cancellationToken);
        return await _db.Tester
            .Where(t => t.ErHjemmeoppgave && t.ErAktiv && t.ErDeltMedPartner
                        && t.OpprettetAvBehandlerId != behandlerId && t.OpprettetAvBehandlerId != null
                        && partnerBehandlerIder.Contains(t.OpprettetAvBehandlerId.Value))
            .OrderByDescending(t => t.OpprettetUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<HashSet<long>> HentLikteTestIderAsync(long behandlerId, CancellationToken cancellationToken = default) =>
        (await _db.HjemmeoppgaveLikinger.Where(l => l.BehandlerId == behandlerId).Select(l => l.TestId).ToListAsync(cancellationToken)).ToHashSet();

    /// <summary>
    /// Bevisst IKKE lagt inn i den delte TestService.LagreSvarAsync (ville påvirket ALLE
    /// eksisterende tester/fyllingsflyter, som i dag stille tillater å hoppe over ethvert ledd —
    /// se CLAUDE.md sin GADIT-fallgruve for hvorfor det er load-bearing andre steder). Kalles KUN
    /// fra Pasientportal/Tester/Fyll.cshtml.cs rett FØR innsending, kun for en
    /// Test.ErHjemmeoppgave-test. Returnerer spørsmålstekstene til ethvert påkrevd, ubesvart ledd —
    /// tom liste betyr "klar til innsending".
    /// </summary>
    public async Task<IReadOnlyList<string>> FinnManglendePaakrevdeAsync(
        long testId, IReadOnlyDictionary<long, string> svar, CancellationToken cancellationToken = default)
    {
        var paakrevdeLedd = await (
            from l in _db.TestLedd
            join s in _db.TestSider on l.TestSideId equals s.Id
            where s.TestId == testId && l.ErPaakrevd
            select l
        ).ToListAsync(cancellationToken);

        return paakrevdeLedd
            .Where(l => !svar.TryGetValue(l.Id, out var v) || string.IsNullOrWhiteSpace(v))
            .Select(l => l.Sporsmalstekst)
            .ToList();
    }
}
