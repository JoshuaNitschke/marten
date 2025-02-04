using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Marten.Storage;
using Marten.Testing.Harness;
using Shouldly;
using Weasel.Postgresql.Tables;
using Xunit;

namespace DocumentDbTests.Bugs;

public class Bug_2914_NGram_index_getting_recreated : BugIntegrationContext
{
    [Fact]
    public async Task do_not_recreate_the_index()
    {
        StoreOptions(opts =>
        {
            opts.Schema.For<NGramDoc>().NgramIndex(x => x.NGramString);
        });

        await theStore.Storage.ApplyAllConfiguredChangesToDatabaseAsync();

        var store2 = SeparateStore(opts =>
        {
            opts.Schema.For<NGramDoc>().NgramIndex(x => x.NGramString);
        });

        await store2.Storage.Database.AssertDatabaseMatchesConfigurationAsync();

        var table = theStore.Storage.Database.AllObjects().OfType<DocumentTable>().Single();

        foreach (var index in table.Indexes)
        {
            Debug.WriteLine(index.ToDDL(table));
        }
    }

    [Fact]
    public void canonicize_SQL()
    {
        var generated = "CREATE INDEX mt_doc_ngramdoc_idx_ngram_ngramstring ON public.mt_doc_ngramdoc USING gin (public.mt_grams_vector( data ->> 'NGramString'));";
        var database =  "CREATE INDEX mt_doc_ngramdoc_idx_ngram_ngramstring ON public.mt_doc_ngramdoc USING gin (public.mt_grams_vector( data ->> 'NGramString'));\n";

        IndexDefinition.CanonicizeDdl(generated).ShouldBe(IndexDefinition.CanonicizeDdl(database));
    }
}


public class NGramDoc
{
    public Guid Id { get; set; }
    public string NGramString { get; set; } = "";
}
