using System;
using Heleus.Apps.Shared;
using Heleus.Apps.HeleusApp.Page.Chain;
using Heleus.Base;
using Heleus.Chain;
using Heleus.Chain.Core;
using Heleus.Chain.Purchases;
using Heleus.Cryptography;
using Xamarin.Forms;

namespace Heleus.Apps.HeleusApp.Views
{
    public class ChainItemView : RowView
    {
        ChainItemStatus _status;
        public ChainItemStatus Status
        {
            get => _status;

            set
            {
                _status = value;
                switch (_status)
                {
                    case ChainItemStatus.New:
                        _statusLabel.TextColor = Color.Green;
                        break;
                    case ChainItemStatus.Revoked:
                        _statusLabel.TextColor = Color.Red;
                        break;
                    case ChainItemStatus.Live:
                        _statusLabel.TextColor = Theme.TextColor.Color;
                        break;
                }
            }
        }

        readonly ExtLabel _statusLabel;

        public ChainItemView(ChainItemStatus status) : base("ChainItemView")
        {
            _statusLabel = new ExtLabel { Text = Tr.Get("ChainItemStatus." + status).ToUpper(), FontStyle = Theme.RowHeaderFont, InputTransparent = true };
            Status = status;

            Children.Add(_statusLabel);
        }
    }

    public class ChainEndPointView : ChainItemView
    {
        public readonly ChainItem<string> EndPoint;

        public ChainEndPointView(ChainItem<string> endPoint) : base(endPoint.Status)
        {
            EndPoint = endPoint;

            AddLastRow("EndPoint", endPoint.Item);
        }
    }

    public class ChainSignedPublicKeyView : ChainItemView
    {
        public readonly ChainKeyItem PublicKey;

        public ChainSignedPublicKeyView(ChainKeyItem publicKey) : base(publicKey.Status)
        {
            PublicKey = publicKey;

            var key = publicKey.Item;
            AddRow("Key", key.PublicKey.HexString);
            AddRow("KeyIndex", key.KeyIndex.ToString());
            AddRow("ChainIndex", key.ChainIndex.ToString());

            AddRow("Admin", key.Flags.HasFlag(PublicChainKeyFlags.ChainAdminKey) ? Tr.Get("Common.DialogYes") : Tr.Get("Common.DialogNo"));
            AddRow("ServiceKey", key.Flags.HasFlag(PublicChainKeyFlags.ServiceChainKey) ? Tr.Get("Common.DialogYes") : Tr.Get("Common.DialogNo"));
            AddRow("ServiceVoteKey", key.Flags.HasFlag(PublicChainKeyFlags.ServiceChainVoteKey) ? Tr.Get("Common.DialogYes") : Tr.Get("Common.DialogNo"));
            AddRow("DataKey", key.Flags.HasFlag(PublicChainKeyFlags.DataChainKey) ? Tr.Get("Common.DialogYes") : Tr.Get("Common.DialogNo"));
            AddRow("DataVoteKey", key.Flags.HasFlag(PublicChainKeyFlags.DataChainVoteKey) ? Tr.Get("Common.DialogYes") : Tr.Get("Common.DialogNo"));
        }
    }

    public class ChainPurchaseView : ChainItemView
    {
        public readonly ChainItem<PurchaseInfo> Purchase;

        public ChainPurchaseView(ChainItem<PurchaseInfo> purchase) : base(purchase.Status)
        {
            Purchase = purchase;

            var p = Purchase.Item;

            AddRow("PurchaseType", Tr.Get("PurchaseTypes." + p.PurchaseType));
            AddRow("PurchaseId", p.PurchaseItemId.ToString());
            AddRow("GroupId", p.PurchaseGroupId.ToString());
            AddRow("Description", p.Description);

            if(p.PurchaseType == PurchaseTypes.Subscription)
            {
                AddRow("Price", Currency.ToString(p.Price));
                AddLastRow("Duration", Time.ToHours(p.Duration).ToString());
            }
            else
            {
                AddLastRow("Price", Currency.ToString(p.Price));
            }
        }
    }
}
