Feature: Artist manager sign-up
  A new artist manager registers from the artist surface, signs in, creates their artist profile.

  @SignUp @ArtistManager
  Scenario: New artist manager registers, signs in, creates their artist profile
    Given a visitor starts sign-up on the artist surface
    When they click the sign up link
    And they register as ArtistManager
    And their email verification completes
    And they sign in with their new credentials
    And they fill in the create artist form
    And they submit the create artist form
    Then they land on the artist surface authenticated
