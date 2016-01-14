// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using MySql.Data.MySqlClient;
using SilverSim.Database.MySQL._Migration;
using SilverSim.Scene.Types.SceneEnvironment;
using SilverSim.Types;
using SilverSim.Types.Agent;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using SilverSim.Types.Parcel;
using SilverSim.Types.Primitive;
using SilverSim.Types.Script;

namespace SilverSim.Database.MySQL.SimulationData
{
    public partial class MySQLSimulationDataStorage
    {
        public void ProcessMigrations()
        {
            using (MySqlConnection conn = new MySqlConnection(m_ConnectionString))
            {
                conn.Open();
                conn.MigrateTables(Migrations_Regions, m_Log);
                conn.MigrateTables(Migrations_Parcels, m_Log);
                conn.MigrateTables(Migrations_Objects, m_Log);
            }
        }

        static readonly IMigrationElement[] Migrations_Regions = new IMigrationElement[]
        {
            #region Table terrains
            new SqlTable("terrains") { Engine = "MyISAM" },
            new AddColumn<UUID>("RegionID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<uint>("PatchID") { IsNullAllowed = false },
            new AddColumn<byte[]>("TerrainData"),
            new PrimaryKeyInfo("RegionID", "PatchID"),
            #endregion

            #region Table environmentsettings
            new SqlTable("environmentsettings") { Engine = "MyISAM" },
            new AddColumn<UUID>("RegionID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<byte[]>("EnvironmentSettings") { IsLong = true },
            new PrimaryKeyInfo("RegionID"),
            #endregion

            #region Table lightshare
            new SqlTable("lightshare"),
            new AddColumn<UUID>("RegionID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<EnvironmentController.WLVector4>("Ambient") { IsNullAllowed = false },
            new AddColumn<EnvironmentController.WLVector4>("CloudColor") { IsNullAllowed = false },
            new AddColumn<double>("CloudCoverage") { IsNullAllowed = false },
            new AddColumn<EnvironmentController.WLVector2>("CloudDetailXYDensity") { IsNullAllowed = false },
            new AddColumn<double>("CloudScale") { IsNullAllowed = false },
            new AddColumn<EnvironmentController.WLVector2>("CloudScroll") { IsNullAllowed = false },
            new AddColumn<bool>("CloudScrollXLock") { IsNullAllowed = false, Default = false },
            new AddColumn<bool>("CloudScrollYLock") { IsNullAllowed = false, Default = false },
            new AddColumn<EnvironmentController.WLVector2>("CloudXYDensity") { IsNullAllowed = false },
            new AddColumn<double>("DensityMultiplier") { IsNullAllowed = false },
            new AddColumn<double>("DistanceMultiplier") { IsNullAllowed = false },
            new AddColumn<bool>("DrawClassicClouds") { IsNullAllowed = false },
            new AddColumn<double>("EastAngle") { IsNullAllowed = false },
            new AddColumn<double>("HazeDensity") { IsNullAllowed = false },
            new AddColumn<double>("HazeHorizon") { IsNullAllowed = false },
            new AddColumn<EnvironmentController.WLVector4>("Horizon") { IsNullAllowed = false },
            new AddColumn<int>("MaxAltitude") { IsNullAllowed = false },
            new AddColumn<double>("StarBrightness") { IsNullAllowed = false },
            new AddColumn<double>("SunGlowFocus") { IsNullAllowed = false },
            new AddColumn<double>("SunGlowSize") { IsNullAllowed = false },
            new AddColumn<double>("SceneGamma") { IsNullAllowed = false },
            new AddColumn<EnvironmentController.WLVector4>("SunMoonColor") { IsNullAllowed = false },
            new AddColumn<double>("SunMoonPosition") { IsNullAllowed = false },
            new AddColumn<EnvironmentController.WLVector2>("BigWaveDirection") { IsNullAllowed = false },
            new AddColumn<EnvironmentController.WLVector2>("LittleWaveDirection") { IsNullAllowed = false },
            new AddColumn<double>("BlurMultiplier") { IsNullAllowed = false },
            new AddColumn<double>("FresnelScale") { IsNullAllowed = false },
            new AddColumn<double>("FresnelOffset") { IsNullAllowed = false },
            new AddColumn<UUID>("NormalMapTexture") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<EnvironmentController.WLVector2>("ReflectionWaveletScale") { IsNullAllowed = false },
            new AddColumn<double>("RefractScaleAbove") { IsNullAllowed = false },
            new AddColumn<double>("RefractScaleBelow") { IsNullAllowed = false },
            new AddColumn<double>("UnderwaterFogModifier") { IsNullAllowed  = false },
            new AddColumn<Color>("WaterColor") { IsNullAllowed = false },
            new AddColumn<double>("FogDensityExponent") { IsNullAllowed = false },
            new PrimaryKeyInfo("RegionID"),
            new TableRevision(2),
            new AddColumn<EnvironmentController.WLVector4>("BlueDensity") { IsNullAllowed = false },
            new TableRevision(3),
            new ChangeColumn<Vector3>("CloudDetailXYDensity") { IsNullAllowed = false },
            new ChangeColumn<Vector3>("CloudXYDensity") { IsNullAllowed = false },
            new ChangeColumn<Vector3>("ReflectionWaveletScale") { IsNullAllowed = false },
            #endregion

            #region Table spawnpoints
            new SqlTable("spawnpoints"),
            new AddColumn<UUID>("RegionID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<Vector3>("Distance") { IsNullAllowed = false },
            new NamedKeyInfo("RegionID", "RegionID"),
            #endregion

            #region Table scriptstates
            new SqlTable("scriptstates"),
            new AddColumn<UUID>("RegionID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UUID>("PrimID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UUID>("ItemID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<string>("ScriptState") { IsLong = true },
            new PrimaryKeyInfo("RegionID", "PrimID", "ItemID"),
            #endregion

            #region Table regionsettings
            new SqlTable("regionsettings"),
            new AddColumn<UUID>("RegionID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<bool>("BlockTerraform") { IsNullAllowed = false , Default = false },
            new AddColumn<bool>("BlockFly") { IsNullAllowed = false, Default = false },
            new AddColumn<bool>("AllowDamage") { IsNullAllowed = false, Default = false },
            new AddColumn<bool>("RestrictPushing") { IsNullAllowed = false, Default = false },
            new AddColumn<bool>("AllowLandResell") { IsNullAllowed = false, Default = true },
            new AddColumn<bool>("AllowLandJoinDivide") { IsNullAllowed = false, Default = true },
            new AddColumn<bool>("BlockShowInSearch") { IsNullAllowed = false, Default = false },
            new AddColumn<int>("AgentLimit") { IsNullAllowed = false, Default = 40 },
            new AddColumn<double>("ObjectBonus") { IsNullAllowed = false, Default = (double)1 },
            new AddColumn<bool>("DisableScripts") { IsNullAllowed = false, Default = false },
            new AddColumn<bool>("DisableCollisions") { IsNullAllowed = false, Default = false },
            new AddColumn<bool>("DisablePhysics") { IsNullAllowed = false, Default = false },
            new AddColumn<bool>("BlockFlyOver") { IsNullAllowed = false, Default = false },
            new AddColumn<bool>("Sandbox") { IsNullAllowed = false, Default = false },
            new AddColumn<UUID>("TerrainTexture1") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UUID>("TerrainTexture2") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UUID>("TerrainTexture3") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UUID>("TerrainTexture4") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UUID>("TelehubObject") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<double>("Elevation1NW") { IsNullAllowed = false, Default = (double)0 },
            new AddColumn<double>("Elevation2NW") { IsNullAllowed = false, Default = (double)0 },
            new AddColumn<double>("Elevation1NE") { IsNullAllowed = false, Default = (double)0 },
            new AddColumn<double>("Elevation2NE") { IsNullAllowed = false, Default = (double)0 },
            new AddColumn<double>("Elevation1SE") { IsNullAllowed = false, Default = (double)0 },
            new AddColumn<double>("Elevation2SE") { IsNullAllowed = false, Default = (double)0 },
            new AddColumn<double>("Elevation1SW") { IsNullAllowed = false, Default = (double)0 },
            new AddColumn<double>("Elevation2SW") { IsNullAllowed = false, Default = (double)0 },
            new AddColumn<double>("WaterHeight") { IsNullAllowed = false, Default = (double)20 },
            new AddColumn<double>("TerrainRaiseLimit") { IsNullAllowed = false, Default = (double)100 },
            new AddColumn<double>("TerrainLowerLimit") { IsNullAllowed = false, Default = (double)-100 },
            new PrimaryKeyInfo("RegionID"),
            new TableRevision(2),
            new AddColumn<bool>("UseEstateSun") { IsNullAllowed = false, Default = true },
            new AddColumn<bool>("IsSunFixed") { IsNullAllowed = false, Default = false },
            new AddColumn<double>("SunPosition") { IsNullAllowed = false, Default = (double)0 },
            #endregion
        };

        static readonly IMigrationElement[] Migrations_Parcels = new IMigrationElement[]
        {
            #region Table parcels
            new SqlTable("parcels"),
            new AddColumn<UUID>("RegionID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UUID>("ParcelID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<int>("LocalID") { IsNullAllowed = false, Default = 0 },
            new AddColumn<byte[]>("Bitmap") { IsLong = true },
            new AddColumn<int>("BitmapWidth") { IsNullAllowed = false, Default = 0 },
            new AddColumn<int>("BitmapHeight") { IsNullAllowed = false, Default = 0 },
            new AddColumn<string>("Name") { Cardinality = 255, IsNullAllowed = false, Default = string.Empty },
            new AddColumn<string>("Description"),
            new AddColumn<UUI>("Owner") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<bool>("IsGroupOwned") { IsNullAllowed = false, Default = false },
            new AddColumn<uint>("Area") { IsNullAllowed = false, Default = (uint)0 },
            new AddColumn<uint>("AuctionID") { IsNullAllowed = false, Default = (uint)0 },
            new AddColumn<UUI>("AuthBuyer") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<ParcelCategory>("Category") { IsNullAllowed = false, Default = ParcelCategory.Any },
            new AddColumn<Date>("ClaimDate") { IsNullAllowed = false, Default = Date.UnixTimeToDateTime(0) },
            new AddColumn<int>("ClaimPrice") { IsNullAllowed = false, Default = 0 },
            new AddColumn<UGI>("Group") { IsNullAllowed = false, Default = UGI.Unknown },
            new AddColumn<ParcelFlags>("Flags") { IsNullAllowed = false, Default = ParcelFlags.None },
            new AddColumn<TeleportLandingType>("LandingType") { IsNullAllowed = false, Default = TeleportLandingType.Anywhere },
            new AddColumn<Vector3>("LandingPosition") { IsNullAllowed = false, Default = Vector3.Zero },
            new AddColumn<Vector3>("LandingLookAt") { IsNullAllowed = false, Default = Vector3.Zero },
            new AddColumn<ParcelStatus>("Status") { IsNullAllowed = false, Default = ParcelStatus.Leased },
            new AddColumn<string>("MusicURI") { Cardinality = 255, IsNullAllowed = false, Default = string.Empty },
            new AddColumn<string>("MediaURI") { Cardinality = 255, IsNullAllowed = false, Default = string.Empty },
            new AddColumn<UUID>("MediaID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UUID>("SnapshotID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<int>("SalePrice") { IsNullAllowed = false, Default = -1 },
            new AddColumn<int>("OtherCleanTime") { IsNullAllowed = false, Default = 0 },
            new AddColumn<bool>("MediaAutoScale") { IsNullAllowed = false, Default = false },
            new AddColumn<int>("RentPrice") { IsNullAllowed = false, Default = 0 },
            new AddColumn<Vector3>("AABBMin") { IsNullAllowed = false, Default = Vector3.Zero },
            new AddColumn<Vector3>("AABBMax") { IsNullAllowed = false, Default = Vector3.Zero },
            new AddColumn<double>("ParcelPrimBonus") { IsNullAllowed = false, Default = (double)1 },
            new AddColumn<int>("PassPrice") { IsNullAllowed = false, Default = 0 },
            new AddColumn<double>("PassHours") { IsNullAllowed = false, Default = (double)0 },
            new AddColumn<uint>("ActualArea") { IsNullAllowed = false, Default = (uint)0 },
            new AddColumn<uint>("BillableArea") { IsNullAllowed = false, Default = (uint)0 },
            new PrimaryKeyInfo("RegionID", "ParcelID"),
            new NamedKeyInfo("ParcelNames", "RegionID", "Name"),
            new NamedKeyInfo("LocalIDs", "RegionID", "LocalID") { IsUnique = true },
            new TableRevision(2),
            new AddColumn<string>("MediaDescription") { Cardinality = 255, IsNullAllowed = false, Default = string.Empty },
            new AddColumn<string>("MediaType") { Cardinality = 255, IsNullAllowed = false, Default = "none/none" },
            new AddColumn<bool>("MediaLoop") { IsNullAllowed = false, Default = false },
            new AddColumn<int>("MediaWidth") { IsNullAllowed = false, Default = 0 },
            new AddColumn<int>("MediaHeight") { IsNullAllowed = false, Default = 0 },
            new TableRevision(3),
            new AddColumn<bool>("ObscureMedia") { IsNullAllowed = false, Default = false },
            new AddColumn<bool>("ObscureMusic") { IsNullAllowed = false, Default = false },
            new TableRevision(4),
            new AddColumn<bool>("SeeAvatars") { IsNullAllowed = false, Default = false },
            new AddColumn<bool>("GroupAvatarSounds") { IsNullAllowed = false, Default = false },
            new AddColumn<bool>("AnyAvatarSounds") { IsNullAllowed = false, Default = false },
            new TableRevision(5),
            new AddColumn<bool>("IsPrivate") { IsNullAllowed = false, Default = false },
            new TableRevision(6),
            /* type corrections */
            new ChangeColumn<uint>("Area") { IsNullAllowed = false, Default = (uint)0 },
            new ChangeColumn<ParcelCategory>("Category") { IsNullAllowed = false, Default = ParcelCategory.Any },
            new ChangeColumn<TeleportLandingType>("LandingType") { IsNullAllowed = false, Default = TeleportLandingType.Anywhere },
            new ChangeColumn<bool>("MediaAutoScale") { IsNullAllowed = false, Default = false },
            new ChangeColumn<bool>("MediaLoop") { IsNullAllowed = false, Default = false },
            new TableRevision(7),
            new ChangeColumn<ParcelStatus>("Status") { IsNullAllowed = false, Default = ParcelStatus.Leased },
            new TableRevision(8),
            new ChangeColumn<int>("Area") { IsNullAllowed = false, Default = 0 },
            new ChangeColumn<int>("ActualArea") { IsNullAllowed = false, Default = 0 },
            new ChangeColumn<int>("BillableArea") { IsNullAllowed = false, Default = 0 },
            new TableRevision(9),
            new ChangeColumn<int>("SalePrice") { IsNullAllowed = false, Default = 0 },
            #endregion

            #region Table parcelaccesswhitelist
            new SqlTable("parcelaccesswhitelist"),
            new AddColumn<UUID>("ParcelID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UUI>("Accessor") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<Date>("ExpiresAt") { IsNullAllowed = false, Default = Date.UnixTimeToDateTime(0) },
            new TableRevision(2),
            new NamedKeyInfo("Accessor", "Accessor"),
            new NamedKeyInfo("ExpiresAt", "ExpiresAt"),
            new TableRevision(3),
            new AddColumn<UUID>("RegionID") { IsNullAllowed = false, Default = UUID.Zero },
            new NamedKeyInfo("RegionID", "RegionID"),
            new TableRevision(4),
            new PrimaryKeyInfo("RegionID", "ParcelID", "Accessor"),
            #endregion

            #region Table parcelaccessblacklist
            new SqlTable("parcelaccessblacklist"),
            new AddColumn<UUID>("ParcelID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UUI>("Accessor") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<Date>("ExpiresAt") { IsNullAllowed = false, Default = Date.UnixTimeToDateTime(0) },
            new TableRevision(2),
            new NamedKeyInfo("Accessor", "Accessor"),
            new NamedKeyInfo("ExpiresAt", "ExpiresAt"),
            new TableRevision(3),
            new AddColumn<UUID>("RegionID") { IsNullAllowed = false, Default = UUID.Zero },
            new NamedKeyInfo("RegionID", "RegionID"),
            new TableRevision(4),
            new PrimaryKeyInfo("RegionID", "ParcelID", "Accessor"),
            #endregion
        };

        static readonly IMigrationElement[] Migrations_Objects = new IMigrationElement[]
        {
            #region Table objects
            new SqlTable("objects") { Engine = "MyISAM" },
            new AddColumn<UUID>("RegionID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UUID>("ID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<bool>("IsVolumeDetect") { IsNullAllowed = false, Default = false },
            new AddColumn<bool>("IsPhantom") { IsNullAllowed = false, Default = false },
            new AddColumn<bool>("IsPhysics") { IsNullAllowed = false, Default = false },
            new AddColumn<bool>("IsTempOnRez") { IsNullAllowed = false, Default = false },
            new AddColumn<UUI>("Owner") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UUI>("LastOwner") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UGI>("Group") { IsNullAllowed = false, Default = UUID.Zero },
            new PrimaryKeyInfo("ID"),
            new TableRevision(2),
            new AddColumn<UUID>("OriginalAssetID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UUID>("NextOwnerAssetID") { IsNullAllowed = false, Default = UUID.Zero },
            new TableRevision(3),
            new AddColumn<int>("Category") { IsNullAllowed = false, Default = 0 },
            new AddColumn<InventoryItem.SaleInfoData.SaleType>("SaleType") { IsNullAllowed = false, Default = InventoryItem.SaleInfoData.SaleType.NoSale },
            new AddColumn<int>("SalePrice") { IsNullAllowed = false, Default = 0 },
            new AddColumn<int>("PayPrice0") { IsNullAllowed = false, Default = 0 },
            new AddColumn<int>("PayPrice1") { IsNullAllowed = false, Default = 0 },
            new AddColumn<int>("PayPrice2") { IsNullAllowed = false, Default = 0 },
            new AddColumn<int>("PayPrice3") { IsNullAllowed = false, Default = 0 },
            new AddColumn<int>("PayPrice4") { IsNullAllowed = false, Default = 0 },
            new TableRevision(4),
            new AddColumn<Vector3>("AttachedPos") { IsNullAllowed = false, Default = Vector3.Zero },
            new AddColumn<AttachmentPoint>("AttachPoint") { IsNullAllowed = false, Default = AttachmentPoint.NotAttached },
            new TableRevision(5),
            new NamedKeyInfo("RegionID", "RegionID"),
            #endregion

            #region Table prims
            new SqlTable("prims") { IsDynamicRowFormat = true, Engine = "MyISAM" },
            new AddColumn<UUID>("ID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UUID>("RootPartID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<int>("LinkNumber") { IsNullAllowed = false, Default = 0 },
            new AddColumn<PrimitiveFlags>("Flags") { IsNullAllowed = false, Default = PrimitiveFlags.None },
            new AddColumn<Vector3>("Position") { IsNullAllowed = false },
            new AddColumn<Quaternion>("Rotation") { IsNullAllowed = false },
            new AddColumn<string>("SitText"),
            new AddColumn<string>("TouchText"),
            new AddColumn<UUI>("Creator") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<Date>("CreationDate") { IsNullAllowed = false, Default = Date.UnixTimeToDateTime(0) },
            new AddColumn<string>("Name") { Cardinality = 64, IsNullAllowed = false, Default = string.Empty },
            new AddColumn<string>("Description") { Cardinality = 255, IsNullAllowed = false, Default = string.Empty },
            new AddColumn<byte[]>("DynAttrs") { IsNullAllowed = false },
            new AddColumn<Vector3>("SitTargetOffset") { IsNullAllowed = false, Default = Vector3.Zero },
            new AddColumn<Quaternion>("SitTargetOrientation") { IsNullAllowed = false, Default = Quaternion.Identity },
            new AddColumn<PrimitivePhysicsShapeType>("PhysicsShapeType") { IsNullAllowed = false, Default = PrimitivePhysicsShapeType.Prim },
            new AddColumn<PrimitiveMaterial>("Material") { IsNullAllowed = false, Default = PrimitiveMaterial.Wood },
            new AddColumn<Vector3>("Size") { IsNullAllowed = false, Default = Vector3.Zero },
            new AddColumn<Vector3>("Slice") { IsNullAllowed = false, Default = Vector3.Zero },
            new AddColumn<string>("MediaURL") { Cardinality = 255, IsNullAllowed = false, Default = string.Empty },
            new AddColumn<Vector3>("AngularVelocity") { IsNullAllowed = false, Default = Vector3.Zero },
            new AddColumn<byte[]>("LightData"),
            new AddColumn<byte[]>("HoverTextData"),
            new AddColumn<byte[]>("FlexibleData"),
            new AddColumn<byte[]>("LoopedSoundData"),
            new AddColumn<byte[]>("ImpactSoundData"),
            new AddColumn<byte[]>("PrimitiveShapeData"),
            new AddColumn<byte[]>("ParticleSystem"),
            new AddColumn<byte[]>("TextureEntryBytes"),
            new AddColumn<int>("ScriptAccessPin") { IsNullAllowed = false, Default = 0},
            new AddColumn<byte[]>("TextureAnimationBytes"),
            new PrimaryKeyInfo("ID", "RootPartID"),
            new NamedKeyInfo("ID", "ID") { IsUnique = true },
            new NamedKeyInfo("RootPartID", "RootPartID"),
            new TableRevision(2),
            new AddColumn<Vector3>("CameraEyeOffset") { IsNullAllowed = false, Default = Vector3.Zero },
            new AddColumn<Vector3>("CameraAtOffset") { IsNullAllowed = false, Default = Vector3.Zero },
            new AddColumn<bool>("ForceMouselook") { IsNullAllowed = false, Default = false },
            new TableRevision(3),
            /* type corrections */
            new ChangeColumn<Date>("CreationDate") { IsNullAllowed = false, Default = Date.UnixTimeToDateTime(0) },
            new TableRevision(4),
            new AddColumn<InventoryPermissionsMask>("BasePermissions") { IsNullAllowed = false, Default = InventoryPermissionsMask.All | InventoryPermissionsMask.Export },
            new AddColumn<InventoryPermissionsMask>("CurrentPermissions") { IsNullAllowed = false, Default = InventoryPermissionsMask.All | InventoryPermissionsMask.Export },
            new AddColumn<InventoryPermissionsMask>("EveryOnePermissions") { IsNullAllowed = false, Default = InventoryPermissionsMask.None },
            new AddColumn<InventoryPermissionsMask>("GroupPermissions") { IsNullAllowed = false, Default = InventoryPermissionsMask.None },
            new AddColumn<InventoryPermissionsMask>("NextOwnerPermissions") { IsNullAllowed = false, Default = InventoryPermissionsMask.None },
            new TableRevision(5),
            new ChangeColumn<byte[]>("LightData") { Cardinality = 255 },
            new ChangeColumn<byte[]>("FlexibleData") { Cardinality = 255 },
            new ChangeColumn<byte[]>("LoopedSoundData") { Cardinality = 255 },
            new ChangeColumn<byte[]>("ImpactSoundData") { Cardinality = 255 },
            new ChangeColumn<byte[]>("PrimitiveShapeData") { Cardinality = 255 },
            new ChangeColumn<byte[]>("ParticleSystem") { Cardinality = 255 },
            new ChangeColumn<byte[]>("TextureAnimationBytes") { Cardinality = 255 },
            new TableRevision(6),
            new AddColumn<UUID>("RegionID") { IsNullAllowed = false, Default = UUID.Zero },
            new NamedKeyInfo("RegionID", "RegionID"),
            new PrimaryKeyInfo("RegionID", "ID", "RootPartID"),
            #endregion

            #region Table primitems
            new SqlTable("primitems") { Engine = "MyISAM" },
            new AddColumn<UUID>("PrimID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UUID>("InventoryID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<string>("Name") { Cardinality = 255, Default = string.Empty },
            new AddColumn<string>("Description") { Cardinality = 255, Default = string.Empty },
            new AddColumn<PrimitiveFlags>("Flags") { IsNullAllowed = false, Default = PrimitiveFlags.None },
            new AddColumn<UUID>("AssetId") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<AssetType>("AssetType") { IsNullAllowed = false, Default = AssetType.Unknown },
            new AddColumn<Date>("CreationDate") { IsNullAllowed = false, Default = Date.UnixTimeToDateTime(0) },
            new AddColumn<UUI>("Creator") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UGI>("Group") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<bool>("GroupOwned") { IsNullAllowed = false },
            new AddColumn<InventoryType>("InventoryType") { IsNullAllowed = false, Default = InventoryType.Unknown },
            new AddColumn<UUI>("LastOwner") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<UUI>("Owner") { IsNullAllowed = false },
            new AddColumn<UUID>("ParentFolderID") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<InventoryPermissionsMask>("BasePermissions") { IsNullAllowed = false, Default = InventoryPermissionsMask.None },
            new AddColumn<InventoryPermissionsMask>("CurrentPermissions") { IsNullAllowed = false, Default = InventoryPermissionsMask.None },
            new AddColumn<InventoryPermissionsMask>("EveryOnePermissions") { IsNullAllowed = false, Default = InventoryPermissionsMask.None },
            new AddColumn<InventoryPermissionsMask>("GroupPermissions") { IsNullAllowed = false, Default = InventoryPermissionsMask.None },
            new AddColumn<InventoryPermissionsMask>("NextOwnerPermissions") { IsNullAllowed = false, Default = InventoryPermissionsMask.None },
            new AddColumn<InventoryItem.SaleInfoData.SaleType>("SaleType") { IsNullAllowed = false, Default = InventoryItem.SaleInfoData.SaleType.NoSale },
            new AddColumn<InventoryPermissionsMask>("SalePermMask") { IsNullAllowed = false, Default = InventoryPermissionsMask.None },
            new PrimaryKeyInfo("PrimID", "InventoryID"),
            new NamedKeyInfo("primID", "PrimID"),
            new TableRevision(2),
            new AddColumn<UUI>("PermsGranter") { IsNullAllowed = false, Default = UUID.Zero },
            new AddColumn<ScriptPermissions>("PermsMask") { IsNullAllowed = false, Default = ScriptPermissions.None },
            new TableRevision(3),
            /* type corrections */
            new ChangeColumn<PrimitiveFlags>("Flags") { IsNullAllowed = false, Default = PrimitiveFlags.None },
            new TableRevision(4),
            /* type corrections */
            new ChangeColumn<Date>("CreationDate") { IsNullAllowed = false, Default = Date.UnixTimeToDateTime(0) },
            new TableRevision(5),
            new AddColumn<UUID>("RegionID") { IsNullAllowed = false, Default = UUID.Zero },
            new NamedKeyInfo("RegionID", "RegionID"),
            new PrimaryKeyInfo("RegionID", "PrimID", "InventoryID"),

            #endregion
        };
    }
}
