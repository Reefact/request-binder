﻿using System;
using System.Collections.Generic;
using System.Linq;

using NFluent;

using Xunit;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedAutoPropertyAccessor.Local
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Local
// ReSharper disable MemberCanBePrivate.Local
// ReSharper disable ClassNeverInstantiated.Local

namespace Reefact.FluentRequestBinder.UnitTests {

    public class Bind_should {

        [Fact]
        public void handle_correctly_when_a_complex_required_property_is_null() {
            // Setup
            Request_v1                   requestWithoutUser = new();
            RequestConverter<Request_v1> bind               = Bind.PropertiesOf(requestWithoutUser);

            // Exercise
            RequiredProperty<User> user = bind.ComplexProperty(r => r.User).AsRequired(ConvertUser!);

            // Verify
            // - property
            Check.That(user.IsValid).IsFalse();

            // - binder
            Check.That(bind.HasError).IsTrue();
            Check.That(bind.ErrorCount).IsEqualTo(1);
            ValidationError error = bind.GetErrors().First();
            Check.That(error.ArgumentName).IsEqualTo("User");
            Check.That(error.ErrorMessage).IsEqualTo("Argument is required.");
        }

        [Fact]
        public void handle_correctly_tree_naming_for_complex_properties() {
            // Setup
            Request_v1 requestWithoutUserName = new() {
                User = new User_v1 {
                    Id = Guid.NewGuid()
                }
            };
            RequestConverter<Request_v1> bind = Bind.PropertiesOf(requestWithoutUserName);

            // Exercise
            RequiredProperty<User> user = bind.ComplexProperty(r => r.User).AsRequired(ConvertUser!);

            // Verify
            // - property
            Check.That(user.IsValid).IsFalse();

            // - binder
            Check.That(bind.HasError).IsTrue();
            Check.That(bind.ErrorCount).IsEqualTo(1);
            ValidationError error = bind.GetErrors().First();
            Check.That(error.ArgumentName).IsEqualTo("User.UserName");
            Check.That(error.ErrorMessage).IsEqualTo("Argument is required.");
        }

        [Fact]
        public void handle_correctly_required_list() {
            // Setup
            Request_v1 requestWitRoles = new();
            requestWitRoles.Roles = new List<Role_v1> {
                new() { Id = "ADM", Name = "Administrator" },
                new() { Id = "DEV", Name = "Developer" }
            };
            RequestConverter<Request_v1> bind = Bind.PropertiesOf(requestWitRoles);

            // Exercise
            RequiredProperty<IEnumerable<Role>> roles = bind.ListOfComplexProperties(r => r.Roles!).AsRequired(ConvertRole);

            // Verify
            Check.That(roles.IsValid).IsTrue();
            Check.That(bind.HasError).IsFalse();
        }

        [Fact]
        public void handle_correctly_missing_required_list() {
            // Setup
            Request_v1 requestWithMissingRoles = new();
            requestWithMissingRoles.Roles = null;
            RequestConverter<Request_v1> bind = Bind.PropertiesOf(requestWithMissingRoles);

            // Exercise
            RequiredProperty<IEnumerable<Role>> roles = bind.ListOfComplexProperties(r => r.Roles!).AsRequired(ConvertRole);

            // Verify
            // - required property
            Check.That(roles.IsValid).IsFalse();
            Check.ThatCode(() => roles.Value)
                 .Throws<InvalidOperationException>()
                 .WithMessage("Property is not valid.");

            // - binder
            Check.That(bind.HasError).IsTrue();
            Check.That(bind.ErrorCount).IsEqualTo(1);

            ValidationError validationError = bind.GetErrors().First();
            Check.That(validationError.ArgumentName).IsEqualTo("Roles");
            Check.That(validationError.ErrorMessage).IsEqualTo("Argument is required.");
        }

        [Fact]
        public void handle_correctly_required_list_having_one_item_invalid() {
            // Setup
            Request_v1 requestWitRoles = new();
            requestWitRoles.Roles = new List<Role_v1> {
                new() { Id = "ADM", Name = "Administrator" },
                new() { Id = "USR" }, // Name is missing
                new() { Id = "DEV", Name = "Developer" }
            };
            RequestConverter<Request_v1> bind = Bind.PropertiesOf(requestWitRoles);

            // Exercise
            RequiredProperty<IEnumerable<Role>> roles = bind.ListOfComplexProperties(r => r.Roles!).AsRequired(ConvertRole);

            // Verify
            // - required property
            Check.That(roles.IsValid).IsFalse();

            // - binder
            Check.That(bind.HasError).IsTrue();
            Check.That(bind.ErrorCount).IsEqualTo(1);

            ValidationError validationError = bind.GetErrors().First();
            Check.That(validationError.ArgumentName).IsEqualTo("Roles[1].Name");
            Check.That(validationError.ErrorMessage).IsEqualTo("Argument is required.");
        }

        [Fact]
        public void handle_correctly_optional_list() {
            // Setup
            Request_v1 requestWitRoles = new();
            requestWitRoles.Roles = new List<Role_v1> {
                new() { Id = "ADM", Name = "Administrator" },
                new() { Id = "DEV", Name = "Developer" }
            };
            RequestConverter<Request_v1> bind = Bind.PropertiesOf(requestWitRoles);

            // Exercise
            OptionalProperty<IEnumerable<Role>> roles = bind.ListOfComplexProperties(r => r.Roles!).AsOptional(ConvertRole);

            // Verify
            // - property
            Check.That(roles.IsValid).IsTrue();
            Check.That(roles.IsMissing).IsFalse();
            Check.That(roles.ArgumentName).IsEqualTo("Roles");
            Check.That(roles.Value).IsEquivalentTo(new Role("ADM", "Administrator"), new Role("DEV", "Developer"));

            // - binder
            Check.That(bind.HasError).IsFalse();
        }

        [Fact]
        public void handle_correctly_missing_optional_list() {
            // Setup
            Request_v1                   requestWitRoles = new();
            RequestConverter<Request_v1> bind            = Bind.PropertiesOf(requestWitRoles);

            // Exercise
            OptionalProperty<IEnumerable<Role>> roles = bind.ListOfComplexProperties(r => r.Roles!).AsOptional(ConvertRole);

            // Verify
            // - property
            Check.That(roles.IsValid).IsTrue();
            Check.That(roles.IsMissing).IsTrue();
            Check.That(roles.Value).IsNotNull();
            Check.That(roles.Value).IsEmpty();

            // - binder
            Check.That(bind.HasError).IsFalse();
        }

        [Fact]
        public void handle_correctly_optional_list_having_one_item_invalid() {
            // Setup
            Request_v1 requestWitRoles = new();
            requestWitRoles.Roles = new List<Role_v1> {
                new() { Id = "ADM", Name = "Administrator" },
                new() { Id = "USR" }, // Name is missing
                new() { Id = "DEV", Name = "Developer" }
            };
            RequestConverter<Request_v1> bind = Bind.PropertiesOf(requestWitRoles);

            // Exercise
            OptionalProperty<IEnumerable<Role>> roles = bind.ListOfComplexProperties(r => r.Roles!).AsOptional(ConvertRole);

            // Verify
            // - required property
            Check.That(roles.IsValid).IsFalse();

            // - binder
            Check.That(bind.HasError).IsTrue();
            Check.That(bind.ErrorCount).IsEqualTo(1);

            ValidationError validationError = bind.GetErrors().First();
            Check.That(validationError.ArgumentName).IsEqualTo("Roles[1].Name");
            Check.That(validationError.ErrorMessage).IsEqualTo("Argument is required.");
        }

        private Role ConvertRole(RequestConverter<Role_v1> bind) {
            RequiredProperty<string> id   = bind.SimpleProperty(x => x.Id!).AsRequired();
            RequiredProperty<string> name = bind.SimpleProperty(x => x.Name!).AsRequired();
            bind.AssertHasNoError();

            return new Role(id, name);
        }

        private User ConvertUser(RequestConverter<User_v1> bind) {
            RequiredProperty<Guid>     id       = bind.SimpleProperty(u => u.Id).AsRequired();
            RequiredProperty<UserName> userName = bind.ComplexProperty(u => u.UserName).AsRequired(ConvertUserName!);
            bind.AssertHasNoError();

            return new User(id, userName);
        }

        private UserName ConvertUserName(RequestConverter<UserName_v1> bind) {
            RequiredProperty<string> firstName = bind.SimpleProperty(x => x.FirstName).AsRequired()!;
            RequiredProperty<string> lastName  = bind.SimpleProperty(x => x.LastName).AsRequired()!;
            bind.AssertHasNoError();

            return new UserName(firstName, lastName);
        }

        #region Nested types declarations

        private class Request_v1 {

            // ReSharper disable once MemberHidesStaticFromOuterClass
            public User_v1?       User  { get; set; }
            public List<Role_v1>? Roles { get; set; }

        }

        private class Role_v1 {

            public string? Id   { get; set; }
            public string? Name { get; set; }

        }

        private class User_v1 {

            public Guid Id { get; set; }
            // ReSharper disable once MemberHidesStaticFromOuterClass
            public UserName_v1? UserName { get; set; }

        }

        private class UserName_v1 {

            public string? FirstName { get; set; }
            public string? LastName  { get; set; }

        }

        public class UserName {

            #region Constructors declarations

            public UserName(string firstName, string lastName) {
                if (firstName is null) { throw new ArgumentNullException(nameof(firstName)); }
                if (lastName is null) { throw new ArgumentNullException(nameof(lastName)); }

                FirstName = firstName;
                LastName  = lastName;
            }

            #endregion

            public string FirstName { get; }
            public string LastName  { get; }

        }

        public sealed class Role : IEquatable<Role> {

            public static bool operator ==(Role? left, Role? right) {
                return Equals(left, right);
            }

            public static bool operator !=(Role? left, Role? right) {
                return !Equals(left, right);
            }

            #region Constructors declarations

            public Role(string id, string name) {
                if (id is null) { throw new ArgumentNullException(nameof(id)); }
                if (name is null) { throw new ArgumentNullException(nameof(name)); }

                Id   = id;
                Name = name;
            }

            #endregion

            public string Id   { get; }
            public string Name { get; }

            /// <inheritdoc />
            public bool Equals(Role? other) {
                if (ReferenceEquals(null, other)) { return false; }
                if (ReferenceEquals(this, other)) { return true; }

                return Id == other.Id && Name == other.Name;
            }

            /// <inheritdoc />
            public override bool Equals(object? obj) {
                return ReferenceEquals(this, obj) || (obj is Role other && Equals(other));
            }

            /// <inheritdoc />
            public override int GetHashCode() {
                return HashCode.Combine(Id, Name);
            }

        }

        private class User {

            #region Constructors declarations

            public User(Guid id, UserName userName) {
                Id       = id;
                UserName = userName;
            }

            #endregion

            public Guid     Id       { get; }
            public UserName UserName { get; }

        }

        #endregion

    }

}