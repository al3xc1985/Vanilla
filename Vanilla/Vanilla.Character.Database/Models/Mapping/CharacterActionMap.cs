namespace Vanilla.Database.Character.Models.Mapping
{
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.ModelConfiguration;

    public class CharacterActionMap : EntityTypeConfiguration<CharacterAction>
    {
        public CharacterActionMap()
        {
            // Primary Key
            this.HasKey(t => new { guid = t.GUID, button = t.Button });

            // Properties
            this.Property(t => t.GUID)
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);

        }
    }
}
