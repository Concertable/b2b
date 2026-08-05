import type { NavigatorScreenParams } from "@react-navigation/native";
import type { ProfileStackParamList } from "@concertable/mobile/navigation/types";

export type MyArtistStackParamList = {
  MyArtistMain: undefined;
};

export type MyVenueStackParamList = {
  MyVenueMain: undefined;
};

export type ArtistTabParamList = {
  Home: undefined;
  Search: undefined;
  MyArtistTab: NavigatorScreenParams<MyArtistStackParamList>;
  Messages: undefined;
  ProfileTab: NavigatorScreenParams<ProfileStackParamList>;
};

export type VenueTabParamList = {
  Home: undefined;
  MyVenueTab: NavigatorScreenParams<MyVenueStackParamList>;
  Messages: undefined;
  ProfileTab: NavigatorScreenParams<ProfileStackParamList>;
};
