﻿namespace Reefact.FluentRequestBinder.UnitTests.__forTesting {

    internal static class RoleConverter {

        #region Statics members declarations

        public static Role Convert(RequestConverter<Role_v1> bind) {
            RequiredReferenceProperty<string> id   = bind.SimpleProperty(x => x.Id!).AsRequired();
            RequiredReferenceProperty<string> name = bind.SimpleProperty(x => x.Name!).AsRequired();
            bind.AssertHasNoError();

            return new Role(id, name);
        }

        #endregion

    }

}