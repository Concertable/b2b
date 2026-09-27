Feature: Venue manager sign-up
  A new venue manager registers from the venue surface, signs in, creates their venue.

  @SignUp @VenueManager
  Scenario: New venue manager registers, signs in, creates their venue
    Given a visitor starts sign-up on the venue surface
    When they click the sign up link
    And they register as VenueManager
    And their email verification completes
    And they sign in with their new credentials
    And they fill in the create venue form
    And they submit the create venue form
    Then they land on the venue surface authenticated
